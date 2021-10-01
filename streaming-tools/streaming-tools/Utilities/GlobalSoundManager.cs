namespace streaming_tools.Utilities {
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Threading;
    using NAudio.Wave;

    /// <summary>
    ///     Handles queuing sounds to play.
    /// </summary>
    public class GlobalSoundManager {
        /// <summary>
        ///     The singleton instance of the class.
        /// </summary>
        private static GlobalSoundManager? instance;

        /// <summary>
        ///     The sentinel that indicates its time to exit the <see cref="soundPlayThread" /> thread.
        /// </summary>
        private readonly SoundPlayingWrapper exitSentinel;

        /// <summary>
        ///     The thread responsible for playing sounds that are queued.
        /// </summary>
        private readonly Thread soundPlayThread;

        /// <summary>
        ///     The collection that contains the sounds to play.
        /// </summary>
        private readonly BlockingCollection<SoundPlayingWrapper> soundsToPlay;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GlobalSoundManager" /> class.
        /// </summary>
        protected GlobalSoundManager() {
            this.exitSentinel = new SoundPlayingWrapper(string.Empty, string.Empty, -1);
            this.soundsToPlay = new BlockingCollection<SoundPlayingWrapper>();
            this.soundPlayThread = new Thread(this.SoundPlayThreadMain);
            this.soundPlayThread.IsBackground = true;
            this.soundPlayThread.Start();
        }

        /// <summary>
        ///     Gets the singleton instance of the class.
        /// </summary>
        public static GlobalSoundManager Instance {
            get {
                if (null == GlobalSoundManager.instance) {
                    GlobalSoundManager.instance = new GlobalSoundManager();
                }

                return GlobalSoundManager.instance;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether we are currently playing sound.
        /// </summary>
        public bool CurrentlyPlayingSound { get; set; }

        /// <summary>
        ///     Adds a sound to play.
        /// </summary>
        /// <param name="filename">The name of the file to play.</param>
        /// <param name="outputDevice">The output device to play the file on.</param>
        /// <param name="volume">The volume to play the file at.</param>
        public void QueueSound(string filename, string outputDevice, int volume) {
            if (string.IsNullOrWhiteSpace(filename) || string.IsNullOrWhiteSpace(outputDevice) || volume < 0 || volume > 100) {
                return;
            }

            this.soundsToPlay.Add(new SoundPlayingWrapper(filename, outputDevice, volume));
        }

        /// <summary>
        ///     The thread that manages playing the sounds.
        /// </summary>
        private void SoundPlayThreadMain() {
            while (true) {
                var sound = this.soundsToPlay.Take();
                if (sound.Equals(this.exitSentinel)) {
                    return;
                }

                if (string.IsNullOrWhiteSpace(sound.Filename) || !File.Exists(sound.Filename)) {
                    continue;
                }

                try {
                    using (var reader = new NAudioUtilities.AudioFileReader(sound.Filename))
                    using (var soundOutputEvent = new WaveOutEvent())
                    using (var signal = new ManualResetEventSlim(false)) {
                        soundOutputEvent.DeviceNumber = NAudioUtilities.GetOutputDeviceIndex(sound.OutputDevice);
                        soundOutputEvent.Volume = sound.Volume / 100f;
                        soundOutputEvent.Init(reader);
                        soundOutputEvent.PlaybackStopped += (_, _) => signal.Set();
                        this.CurrentlyPlayingSound = true;
                        soundOutputEvent.Play();
                        signal.Wait();
                        this.CurrentlyPlayingSound = false;
                    }
                } catch (Exception) { }
            }
        }

        /// <summary>
        ///     A wrapper that contains all of the information we need to play a sound.
        /// </summary>
        public class SoundPlayingWrapper {
            /// <summary>
            ///     The name of the file.
            /// </summary>
            public string Filename;

            /// <summary>
            ///     The output device to stream to.
            /// </summary>
            public string OutputDevice;

            /// <summary>
            ///     The volume to play the file at.
            /// </summary>
            public int Volume;

            /// <summary>
            ///     Initializes a new instance of the <see cref="SoundPlayingWrapper" /> class.
            /// </summary>
            /// <param name="filename">The name of the file.</param>
            /// <param name="outputDevice">The output device to stream to.</param>
            /// <param name="volume">The volume to play the file at.</param>
            public SoundPlayingWrapper(string filename, string outputDevice, int volume) {
                this.Filename = filename;
                this.OutputDevice = outputDevice;
                this.Volume = volume;
            }
        }
    }
}