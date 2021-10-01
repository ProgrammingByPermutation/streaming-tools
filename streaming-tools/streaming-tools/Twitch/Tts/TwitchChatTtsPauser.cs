namespace streaming_tools.Twitch.Tts {
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Timers;
    using JetBrains.Annotations;
    using NAudio.Wave;
    using NAudio.Wave.SampleProviders;
    using Utilities;

    /// <summary>
    ///     Handles pausing twitch chat TTS when the microphone hears.
    /// </summary>
    public class TwitchChatTtsPauser : INotifyPropertyChanged {
        /// <summary>
        ///     The timer used to continue TTS at some point after microphone data is detected.
        /// </summary>
        private readonly Timer unpauseTimer;

        /// <summary>
        ///     The object that buffers that data from the microphone.
        /// </summary>
        private BufferedWaveProvider? microphoneBufferedData;

        /// <summary>
        ///     The main event that listens for microphone data.
        /// </summary>
        private WaveInEvent? microphoneDataEvent;

        /// <summary>
        ///     The current volume of the voice read from the microphone.
        /// </summary>
        private int microphoneMicrophoneVoiceVolume;

        /// <summary>
        ///     The object that converts the microphone data into a wave so we can read the volume.
        /// </summary>
        private MeteringSampleProvider? microphoneVoiceData;

        /// <summary>
        ///     The percentage of volume at which we should pause TTS when we detect microphone data.
        /// </summary>
        private int pauseThreshold;

        /// <summary>
        ///     The string representation of the microphone device name from NAudio.
        /// </summary>
        private int selectedMicrophone;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TwitchChatTtsPauser" /> class.
        /// </summary>
        public TwitchChatTtsPauser() {
            // Default to unpausing TTS 1 second after the microphone threshold has paused it.
            this.unpauseTimer = new Timer(1000);
            this.unpauseTimer.Elapsed += this.UnpauseTimer_Elapsed;
            this.unpauseTimer.AutoReset = false;
        }

        /// <summary>
        ///     Gets or sets the current volume of the voice read from the microphone.
        /// </summary>
        public int MicrophoneVoiceVolume {
            get => this.microphoneMicrophoneVoiceVolume;
            set {
                this.microphoneMicrophoneVoiceVolume = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the percentage of volume at which we should pause TTS when we detect microphone data.
        /// </summary>
        public int PauseThreshold {
            get => this.pauseThreshold;
            set {
                if (value < 0) {
                    value = 0;
                } else if (value > 100) {
                    value = 100;
                }

                this.pauseThreshold = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the string representation of the microphone device name from NAudio.
        /// </summary>
        public int SelectedMicrophone {
            get => this.selectedMicrophone;
            set {
                this.selectedMicrophone = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the reference to the TTS class the reads twitch chat.
        /// </summary>
        public TwitchChatTts? Tts { get; set; }

        /// <summary>
        ///     Invoked when properties on the class change.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        ///     Gets the NAudio index associated with the selected microphone.
        /// </summary>
        /// <param name="guid">The unique GUID of the microphone.</param>
        /// <returns>The index of the microphone according to NAudio.</returns>
        public static int GetSelectMicrophoneDeviceIndex(string? guid) {
            if (null == guid) {
                return -1;
            }

            var totalDevices = NAudioUtilities.GetTotalInputDevices();
            var index = -1;
            for (var i = -1; i < totalDevices; i++) {
                if (NAudioUtilities.GetInputDevice(i).ProductGuid.ToString() == guid) {
                    index = i + 1;
                    break;
                }
            }

            return index;
        }

        /// <summary>
        ///     Starts listening to the microphone so we know when to pause TTS for microphone speaking.
        /// </summary>
        public void StartListenToMicrophone() {
            if (-1 == this.SelectedMicrophone) {
                return;
            }

            this.microphoneDataEvent = new WaveInEvent { DeviceNumber = this.SelectedMicrophone - 1 };
            this.microphoneDataEvent.DataAvailable += this.Microphone_DataReceived;
            this.microphoneBufferedData = new BufferedWaveProvider(new WaveFormat(8000, 1));

            var sampleChannel = new SampleChannel(this.microphoneBufferedData, true);
            sampleChannel.PreVolumeMeter += this.MicrophoneAudioChannel_PreVolumeMeter;
            sampleChannel.Volume = 100;
            this.microphoneVoiceData = new MeteringSampleProvider(sampleChannel);

            // microphoneVoiceData.StreamVolume += PostVolumeMeter_StreamVolume;

            this.microphoneDataEvent.StartRecording();
        }

        /// <summary>
        ///     Stops listening to the microphone.
        /// </summary>
        public void StopListenToMicrophone() {
            this.microphoneDataEvent?.Dispose();
            this.microphoneDataEvent = null;

            this.microphoneBufferedData?.ClearBuffer();
            this.microphoneBufferedData = null;
            this.microphoneVoiceData = null;
        }

        /// <summary>
        ///     Raised when a property is changed on the class.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        ///     Called when the microphone creates voice data.
        /// </summary>
        /// <param name="sender">The <seealso cref="WaveInEvent" /> that captured the data from the microphone.</param>
        /// <param name="e">Data received.</param>
        private void Microphone_DataReceived(object? sender, WaveInEventArgs e) {
            if (null == this.microphoneBufferedData || null == this.microphoneVoiceData) {
                return;
            }

            this.microphoneBufferedData.AddSamples(e.Buffer, 0, e.BytesRecorded);
            float[] test = new float[e.Buffer.Length];
            this.microphoneVoiceData.Read(test, 0, e.BytesRecorded);
        }

        /// <summary>
        ///     Called when microphone voice data is recognized.
        /// </summary>
        /// <param name="sender">A <seealso cref="SampleChannel" /> receiving microphone data.</param>
        /// <param name="e">The data on how loud the voice is.</param>
        private void MicrophoneAudioChannel_PreVolumeMeter(object? sender, StreamVolumeEventArgs e) {
            this.MicrophoneVoiceVolume = Convert.ToInt32(e.MaxSampleValues[0] * 100);

            if (this.MicrophoneVoiceVolume > this.PauseThreshold && null != this.Tts) {
                this.Tts.Pause();
                this.unpauseTimer.Stop();
                this.unpauseTimer.Start();
            }
        }

        /// <summary>
        ///     Event fired after TTS has been paused to continue it. This occurs after the TTS
        ///     has been paused by the voice detected in the microphone has exceeded the threshold.
        /// </summary>
        /// <param name="sender">The timer.</param>
        /// <param name="e">The event arguments.</param>
        private void UnpauseTimer_Elapsed(object sender, ElapsedEventArgs e) {
            this.Tts?.Unpause();
        }
    }
}