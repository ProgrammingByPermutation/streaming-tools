namespace streaming_tools.ViewModels {
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Speech.Synthesis;
    using System.Timers;
    using DynamicData;
    using ReactiveUI;
    using Twitch;
    using Twitch.Admin;
    using Twitch.Tts;
    using Utilities;
    using Views;

    /// <summary>
    ///     The view responsible for a single entry in the twitch chat configuration list.
    /// </summary>
    public class TwitchChatConfigViewModel : ViewModelBase, IDisposable {
        /// <summary>
        ///     Check if a chat is currently connected.
        /// </summary>
        private readonly Timer isConnectedTimer;

        /// <summary>
        ///     The twitch chat administration singleton for registering for twitch chat callbacks.
        /// </summary>
        private TwitchChatAdmin? admin;

        /// <summary>
        ///     True if the bot should provide administration commands, false otherwise.
        /// </summary>
        private bool adminOn;

        /// <summary>
        ///     The twitch chat configuration object from the persistent configuration.
        /// </summary>
        private TwitchChatConfiguration? chatConfig;

        /// <summary>
        ///     A value indicating if a chat is connected.
        /// </summary>
        private bool isConnected;

        /// <summary>
        ///     The output device to send TTS to.
        /// </summary>
        private string? outputDevice;

        /// <summary>
        ///     True if the bot should pause TTS based on microphone data, false otherwise.
        /// </summary>
        private bool pauseDuringSpeech;

        /// <summary>
        ///     The object responsible for pausing TTS based on microphone data.
        /// </summary>
        private TwitchChatTtsPauser? pauseObject;

        /// <summary>
        ///     The TTS object that turns text to speech.
        /// </summary>
        private TwitchChatTts? tts;

        /// <summary>
        ///     True if TTS is on, false otherwise.
        /// </summary>
        private bool ttsOn;

        /// <summary>
        ///     The voice to use for TTS.
        /// </summary>
        private string? ttsVoice;

        /// <summary>
        ///     The volume of TTS on the output device.
        /// </summary>
        private uint ttsVolume;

        /// <summary>
        ///     The twitch channel to connect to.
        /// </summary>
        private string? twitchChannel;

        /// <summary>
        ///     The username to connect to the <see cref="twitchChannel" /> with.
        /// </summary>
        private string? username;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TwitchChatConfigViewModel" /> class.
        /// </summary>
        public TwitchChatConfigViewModel() {
            this.Config = Configuration.Instance;
            this.isConnectedTimer = new Timer(500);
            this.isConnectedTimer.AutoReset = true;
            this.isConnectedTimer.Elapsed += this.IsConnectedTimer_OnElapsed;

            // Create the list of TTS voices.
            var speech = new SpeechSynthesizer();
            this.TtsVoices = new ObservableCollection<string>();
            this.TtsVoices.AddRange(speech.GetInstalledVoices().Select(v => v.VoiceInfo.Name));
            speech.Dispose();

            // Create the list of Output devices.
            var outputDevices = NAudioUtilities.GetTotalOutputDevices();
            this.OutputDevices = new ObservableCollection<string>();
            this.OutputDevices.AddRange(Enumerable.Range(-1, outputDevices + 1).Select(n => NAudioUtilities.GetOutputDevice(n).ProductName));

            // Setup our listeners.
            this.PropertyChanged += this.OnPropertyChanged;
            this.isConnectedTimer.Start();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TwitchChatConfigViewModel" /> class.
        /// </summary>
        /// <param name="config">The twitch chat configuration to base the row on.</param>
        public TwitchChatConfigViewModel(TwitchChatConfiguration config)
            : this() {
            this.PropertyChanged -= this.OnPropertyChanged;
            this.chatConfig = config;
            this.CopyFromConfig(config);
            this.PropertyChanged += this.OnPropertyChanged;
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the bot should provide administration commands.
        /// </summary>
        public bool AdminOn {
            get => this.adminOn;
            set {
                if (value == this.adminOn) {
                    return;
                }

                this.RaiseAndSetIfChanged(ref this.adminOn, value);

                if (value) {
                    this.admin = new TwitchChatAdmin(this.chatConfig);
                    this.admin.Connect();
                } else {
                    this.admin?.Dispose();
                    this.admin = null;
                }
            }
        }

        /// <summary>
        ///     Gets or sets a delegate for deleting this configuration object when the UI is clicked.
        /// </summary>
        public Action? DeleteConfig { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether a chat is connected.
        /// </summary>
        public bool IsConnected {
            get => this.isConnected;
            set => this.RaiseAndSetIfChanged(ref this.isConnected, value);
        }

        /// <summary>
        ///     Gets or sets the output device to send audio to.
        /// </summary>
        public string? OutputDevice {
            get => this.outputDevice;
            set => this.RaiseAndSetIfChanged(ref this.outputDevice, value);
        }

        /// <summary>
        ///     Gets or sets the collection of possible output devices on the machine.
        /// </summary>
        public ObservableCollection<string> OutputDevices { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether text to speech should pause when someone talks into the microphone.
        /// </summary>
        public bool PauseDuringSpeech {
            get => this.pauseDuringSpeech;
            set {
                if (value == this.pauseDuringSpeech) {
                    return;
                }

                this.RaiseAndSetIfChanged(ref this.pauseDuringSpeech, value);

                if (value) {
                    this.pauseObject = new TwitchChatTtsPauser();
                    this.pauseObject.SelectedMicrophone = TwitchChatTtsPauser.GetSelectMicrophoneDeviceIndex(this.Config.MicrophoneGuid);
                    this.pauseObject.Tts = this.tts;
                    this.pauseObject.PauseThreshold = this.Config.PauseThreshold;
                    this.pauseObject.StartListenToMicrophone();
                } else {
                    this.pauseObject?.StopListenToMicrophone();
                    this.pauseObject = null;
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether text to speech is on.
        /// </summary>
        public bool TtsOn {
            get => this.ttsOn;
            set {
                if (value == this.ttsOn) {
                    return;
                }

                this.RaiseAndSetIfChanged(ref this.ttsOn, value);

                if (value) {
                    this.tts = new TwitchChatTts(this.chatConfig);
                    this.tts.Connect();

                    if (null != this.pauseObject) {
                        this.pauseObject.Tts = this.tts;
                    }
                } else {
                    if (null != this.pauseObject) {
                        this.pauseObject.Tts = null;
                    }

                    this.tts?.Dispose();
                    this.tts = null;
                }
            }
        }

        /// <summary>
        ///     Gets or sets the selected Microsoft Text to Speech voice.
        /// </summary>
        public string? TtsVoice {
            get => this.ttsVoice;
            set => this.RaiseAndSetIfChanged(ref this.ttsVoice, value);
        }

        /// <summary>
        ///     Gets or sets the collection of possible TTS voices from Microsoft OS.
        /// </summary>
        public ObservableCollection<string> TtsVoices { get; set; }

        /// <summary>
        ///     Gets or sets the volume of the text to speech voice.
        /// </summary>
        public uint TtsVolume {
            get => this.ttsVolume;
            set => this.RaiseAndSetIfChanged(ref this.ttsVolume, value);
        }

        /// <summary>
        ///     Gets or sets the twitch channel to read chat from.
        /// </summary>
        public string? TwitchChannel {
            get => this.twitchChannel;
            set => this.RaiseAndSetIfChanged(ref this.twitchChannel, value);
        }

        /// <summary>
        ///     Gets or sets the twitch username.
        /// </summary>
        public string? Username {
            get => this.username;
            set => this.RaiseAndSetIfChanged(ref this.username, value);
        }

        /// <summary>
        ///     Gets the persistent configuration object.
        /// </summary>
        private Configuration Config { get; }

        /// <summary>
        ///     Disposes of unmanaged resources.
        /// </summary>
        public void Dispose() {
            this.tts?.Dispose();
            this.tts = null;
            this.PropertyChanged -= this.OnPropertyChanged;
            this.isConnectedTimer.Stop();
            this.isConnectedTimer.Dispose();
        }

        /// <summary>
        ///     Invokes the <see cref="DeleteConfig" /> passed to us from a parent object.
        /// </summary>
        public void DeleteConfigCommand() {
            this.DeleteConfig?.Invoke();
        }

        /// <summary>
        ///     Copies the values from the persistent configuration object to this view.
        /// </summary>
        /// <param name="config">The persistent configuration object.</param>
        private void CopyFromConfig(TwitchChatConfiguration? config) {
            if (null == config) {
                return;
            }

            this.Username = config.AccountUsername;
            this.AdminOn = config.AdminOn;
            this.TwitchChannel = config.TwitchChannel;
            this.TtsVoice = config.TtsVoice;
            this.OutputDevice = config.OutputDevice;
            this.PauseDuringSpeech = config.PauseDuringSpeech;
            this.TtsVolume = config.TtsVolume;
            this.TtsOn = config.TtsOn;
        }

        /// <summary>
        ///     Copies the values from this view to the persistent configuration object and writes it to disk.
        /// </summary>
        /// <param name="config">The persistent configuration object.</param>
        private void CopyToConfig(TwitchChatConfiguration? config) {
            if (null == config) {
                return;
            }

            config.AccountUsername = this.Username;
            config.AdminOn = this.AdminOn;
            config.TwitchChannel = this.TwitchChannel;
            config.TtsVoice = this.TtsVoice;
            config.OutputDevice = this.OutputDevice;
            config.PauseDuringSpeech = this.PauseDuringSpeech;
            config.TtsOn = this.TtsOn;
            config.TtsVolume = this.TtsVolume;

            this.Config.WriteConfiguration();
        }

        /// <summary>
        ///     Checks if the twitch chat associated with this configuration is connected.
        /// </summary>
        /// <param name="sender">The timer.</param>
        /// <param name="e">The event arguments.</param>
        private void IsConnectedTimer_OnElapsed(object sender, ElapsedEventArgs e) {
            if (string.IsNullOrWhiteSpace(this.chatConfig?.AccountUsername) || string.IsNullOrWhiteSpace(this.chatConfig.TwitchChannel)) {
                return;
            }

            var manager = TwitchChatManager.Instance;
            this.IsConnected = manager.TwitchChannelIsConnected(this.chatConfig.AccountUsername, this.chatConfig.TwitchChannel);
        }

        /// <summary>
        ///     Raised when a property changes on this object.
        /// </summary>
        /// <param name="sender">This object.</param>
        /// <param name="e">The property changed information.</param>
        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) {
            if (null == this.Config.TwitchChatConfigs) {
                return;
            }

            if (null == this.chatConfig) {
                this.chatConfig = this.Config.TwitchChatConfigs.FirstOrDefault(c => null != c?.TwitchChannel && c.TwitchChannel.Equals(this.TwitchChannel, StringComparison.InvariantCultureIgnoreCase));
            }

            if (null == this.chatConfig) {
                this.chatConfig = new TwitchChatConfiguration();
                this.Config.TwitchChatConfigs.Add(this.chatConfig);
            }

            this.CopyToConfig(this.chatConfig);
        }
    }
}