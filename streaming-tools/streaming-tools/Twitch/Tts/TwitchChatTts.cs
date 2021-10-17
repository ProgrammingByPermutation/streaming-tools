namespace streaming_tools.Twitch.Tts {
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Speech.Synthesis;
    using System.Threading;
    using NAudio.Wave;
    using TtsFilter;
    using TwitchLib.Client;
    using TwitchLib.Client.Events;
    using Utilities;

    /// <summary>
    ///     A twitch chat Text-to-speech client.
    /// </summary>
    public class TwitchChatTts : IDisposable {
        /// <summary>
        ///     The configuration for the twitch chat.
        /// </summary>
        public TwitchChatConfiguration? ChatConfig { get; }

        /// <summary>
        ///     The collection of sounds to play.
        /// </summary>
        private readonly BlockingCollection<OnMessageReceivedArgs> soundsToPlay;

        /// <summary>
        ///     The thread to play sounds on.
        /// </summary>
        private readonly Thread soundThread;

        /// <summary>
        ///     Filters for modifying an incoming message for text to speech.
        /// </summary>
        private readonly ITtsFilter[] ttsFilters = { new LinkFilter(), new UsernameSkipFilter(), new UsernameRemoveCharactersFilter(), new PhoneticFilter(), new CommandFilter(), new EmojiDeduplicationFilter(), new WordSpamFilter() };

        /// <summary>
        ///     The lock for ensuring mutual exclusion on the <see cref="ttsSoundOutput" /> object.
        /// </summary>
        private readonly object ttsSoundOutputLock = new();

        /// <summary>
        ///     The lock for ensuring mutual exclusion on the <see cref="ttsSoundOutputSignal" /> object.
        /// </summary>
        private readonly object ttsSoundOutputSignalLock = new();

        /// <summary>
        ///     The poison pill to kill the sound thread.
        /// </summary>
        private bool poisonPill;

        /// <summary>
        ///     The text-to-speech sound output.
        /// </summary>
        private WaveOutEvent? ttsSoundOutput;

        /// <summary>
        ///     The signal used to make sound output synchronous.
        /// </summary>
        private ManualResetEvent? ttsSoundOutputSignal;

        private int messageToSkip;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TwitchChatTts" /> class.
        /// </summary>
        /// <param name="config">The configuration for the twitch chat.</param>
        public TwitchChatTts(TwitchChatConfiguration? config) {
            this.soundsToPlay = new BlockingCollection<OnMessageReceivedArgs>();
            this.soundThread = new Thread(this.PlaySoundsThread);
            this.soundThread.Name = "TwitchChatTts Thread";
            this.soundThread.IsBackground = true;
            this.soundThread.Start();
            this.ChatConfig = config;
        }

        /// <summary>
        ///     Releases unmanaged resources.
        /// </summary>
        public void Dispose() {
            this.poisonPill = true;
            this.soundsToPlay.Add(new OnMessageReceivedArgs());

            lock (this.ttsSoundOutputLock) {
                this.ttsSoundOutputSignal?.Set();
                this.ttsSoundOutputSignal?.Dispose();
                this.ttsSoundOutputSignal = null;
            }

            lock (this.ttsSoundOutputSignalLock) {
                this.ttsSoundOutput?.Stop();
                this.ttsSoundOutput?.Dispose();
                this.ttsSoundOutput = null;
            }

            if (null == this.ChatConfig) {
                return;
            }

            var user = Configuration.Instance.GetTwitchAccount(this.ChatConfig.AccountUsername);
            if (null == user) {
                return;
            }

            var twitchManager = TwitchChatManager.Instance;
            twitchManager.RemoveTwitchChannel(user, this.ChatConfig.TwitchChannel, this.Client_OnMessageReceived);
            if (this.soundThread.Join(5000)) {
                this.soundThread.Interrupt();
            }
        }
        
        public string? CurrentUsername { get; set; }

        /// <summary>
        ///     The main thread used to play sound asynchronously.
        /// </summary>
        private void PlaySoundsThread() {
            while (!this.poisonPill) {
                try {
                    foreach (var e in this.soundsToPlay.GetConsumingEnumerable()) {
                        if (this.poisonPill) {
                            return;
                        }

                        if (messageToSkip > 0) {
                            --messageToSkip;
                            Console.WriteLine($"Skipping: {e.ChatMessage.Username} says {e.ChatMessage.Message}");
                            continue;
                        }

                        Console.WriteLine($"Running: {e.ChatMessage.Username} says {e.ChatMessage.Message}");
                        if (null == this.ChatConfig) {
                            continue;
                        }

                        // Go through the TTS filters which modify the chat message and update it.
                        var chatMessageInfo = new Tuple<string, string>(e.ChatMessage.DisplayName, e.ChatMessage.Message);
                        foreach (var filter in this.ttsFilters) {
                            if (null == chatMessageInfo) {
                                break;
                            }

                            chatMessageInfo = filter.Filter(e, chatMessageInfo.Item1, chatMessageInfo.Item2);
                        }

                        // If we don't have a chat message then the message was completely filtered out and we have nothing
                        // to do here.
                        if (null == chatMessageInfo || string.IsNullOrWhiteSpace(chatMessageInfo.Item2.Trim())) {
                            continue;
                        }

                        // If the chat message starts with the !tts command, then TTS is supposed to read the message as if
                        // they're say it. So we will handle the message as such.
                        string chatMessage;
                        if (!chatMessageInfo.Item2.Trim().StartsWith("!tts", StringComparison.InvariantCultureIgnoreCase)) {
                            chatMessage = $"{chatMessageInfo.Item1} says {chatMessageInfo.Item2}";
                        } else {
                            chatMessage = chatMessageInfo.Item2.Replace("!tts", "");
                        }

                        // Create a microsoft TTS object and a stream for outputting its audio file to.
                        using (var synth = new SpeechSynthesizer())
                        using (var stream = new MemoryStream()) {
                            // Setup the microsoft TTS object according to the settings.
                            synth.SetOutputToWaveStream(stream);
                            synth.SelectVoice(this.ChatConfig.TtsVoice);
                            synth.Volume = (int)this.ChatConfig.TtsVolume;
                            synth.Speak(chatMessage);

                            // Now that we filled the stream, seek to the beginning so we can play it.
                            stream.Seek(0, SeekOrigin.Begin);
                            var reader = new WaveFileReader(stream);

                            while (GlobalSoundManager.Instance.CurrentlyPlayingSound) {
                                Thread.Sleep(100);
                            }

                            try {
                                // Make sure we lock the objects used on multiple threads and play the file.
                                lock (this.ttsSoundOutputLock)
                                lock (this.ttsSoundOutputSignalLock) {
                                    this.ttsSoundOutput = new WaveOutEvent();
                                    this.ttsSoundOutputSignal = new ManualResetEvent(false);

                                    this.ttsSoundOutput.DeviceNumber = NAudioUtilities.GetOutputDeviceIndex(this.ChatConfig.OutputDevice);
                                    this.ttsSoundOutput.Volume = this.ChatConfig.TtsVolume / 100.0f;

                                    this.ttsSoundOutput.Init(reader);

                                    // Play is async so we will make it synchronous here so we don't have to deal with
                                    // queueing. We can improve this to remove the hack in the future.
                                    this.ttsSoundOutput.PlaybackStopped += delegate {
                                        lock (this.ttsSoundOutputSignalLock) {
                                            this.ttsSoundOutputSignal?.Set();
                                        }
                                    };

                                    // Play it.
                                    this.ttsSoundOutput.Play();
                                }

                                // Wait for the play to finish, we will get signaled.
                                CurrentUsername = e.ChatMessage.Username;
                                var signal = this.ttsSoundOutputSignal;
                                signal?.WaitOne();
                                CurrentUsername = null;
                            } finally {
                                // Finally dispose of everything safely in the lock.
                                lock (this.ttsSoundOutputLock)
                                lock (this.ttsSoundOutputSignalLock) {
                                    this.ttsSoundOutput?.Dispose();
                                    this.ttsSoundOutput = null;
                                    this.ttsSoundOutputSignal?.Dispose();
                                    this.ttsSoundOutputSignal = null;
                                }
                            }
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Got expection playing message: {ex}");
                }
            }
        }

        /// <summary>
        ///     Connects to the chat to listen for messages to read in text to speech.
        /// </summary>
        public void Connect() {
            if (null == this.ChatConfig) {
                return;
            }

            var config = Configuration.Instance;
            var user = config.GetTwitchAccount(this.ChatConfig.AccountUsername);
            if (null == user) {
                return;
            }

            var twitchManager = TwitchChatManager.Instance;
            twitchManager.AddTwitchChannel(user, this.ChatConfig.TwitchChannel, this.Client_OnMessageReceived);
        }

        /// <summary>
        ///     Pauses the text to speech.
        /// </summary>
        public void Pause() {
            lock (this.ttsSoundOutputLock) {
                this.ttsSoundOutput?.Pause();
            }
        }

        /// <summary>
        ///     Continues the text to speech.
        /// </summary>
        public void Unpause() {
            lock (this.ttsSoundOutputLock) {
                this.ttsSoundOutput?.Play();
            }
        }

        /// <summary>
        ///     Event called when a message in received in twitch chat.
        /// </summary>
        /// <param name="twitchClient">The twitch chat client.</param>
        /// <param name="e">The chat message information.</param>
        private void Client_OnMessageReceived(TwitchClient twitchClient, OnMessageReceivedArgs e) {
            Console.WriteLine($"Adding: {e.ChatMessage.Username} says {e.ChatMessage.Message}");
            try {
                this.soundsToPlay.Add(e);
            } catch (Exception ex) {
                Console.WriteLine($"Failed to add: {e.ChatMessage.Username} says {e.ChatMessage.Message}\r\n{e}");
            }
        }

        public void SkipCurrentTts() {
            this.ttsSoundOutput?.Stop();
        }
        
        public void SkipAllTts() {
            messageToSkip = this.soundsToPlay.Count;
            this.ttsSoundOutput?.Stop();
        }
    }
}