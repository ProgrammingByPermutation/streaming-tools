namespace streaming_tools.ViewModels {
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using Avalonia;
    using DynamicData;
    using ReactiveUI;
    using Twitch.Tts;
    using Utilities;

    /// <summary>
    ///     The view responsible for pausing twitch chat TTS when the microphone hears things.
    /// </summary>
    public class TtsPauseConfigViewModel : ViewModelBase {
        /// <summary>
        ///     The persisted configuration.
        /// </summary>
        private readonly Configuration config;

        /// <summary>
        ///     A reference to the object that encapsulates reading microphone volume
        ///     in order to pause TTS.
        /// </summary>
        private readonly TwitchChatTtsPauser pausingObject = new();

        /// <summary>
        ///     The margin to use on the visual indicator for the pause threshold.
        /// </summary>
        private Thickness microphoneMicrophoneThresholdVisualizationMargin;

        /// <summary>
        ///     The current volume of the voice read from the microphone.
        /// </summary>
        private int microphoneMicrophoneVoiceVolume;

        /// <summary>
        ///     The 0% - 100% microphone  at which to pause TTS.
        /// </summary>
        private int pauseThreshold;

        /// <summary>
        ///     The string representation of the microphone device name from NAudio.
        /// </summary>
        private int selectedMicrophone;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TtsPauseConfigViewModel" /> class.
        /// </summary>
        public TtsPauseConfigViewModel() {
            // Get the configuration and assign the values.
            this.config = Configuration.Instance;

            this.SelectedMicrophone = TwitchChatTtsPauser.GetSelectMicrophoneDeviceIndex(this.config.MicrophoneGuid);
            this.PauseThreshold = this.config.PauseThreshold;

            // We listen to our own property changed event to know when we need to push
            // information to the TTS object.
            this.PropertyChanged += this.OnPropertyChanged;

            // Setup the list of microphone sources
            var devices = NAudioUtilities.GetTotalInputDevices();
            var list = Enumerable.Range(-1, devices + 1).Select(n => NAudioUtilities.GetInputDevice(n).ProductName).ToArray();
            this.MicrophoneDevices = new ObservableCollection<string>();
            this.MicrophoneDevices.AddRange(list);
            this.pausingObject.PropertyChanged += this.Pauser_PropertyChanged;
            this.pausingObject.SelectedMicrophone = this.SelectedMicrophone;
            this.pausingObject.StartListenToMicrophone();
        }

        /// <summary>
        ///     Gets or sets the collection of microphone devices currently attached to the computer.
        /// </summary>
        public ObservableCollection<string> MicrophoneDevices { get; set; }

        /// <summary>
        ///     Gets or sets the margin used to push the visual representation of the threshold marker on the UI.
        /// </summary>
        public Thickness MicrophoneThresholdVisualizationMargin {
            get => this.microphoneMicrophoneThresholdVisualizationMargin;
            set => this.RaiseAndSetIfChanged(ref this.microphoneMicrophoneThresholdVisualizationMargin, value);
        }

        /// <summary>
        ///     Gets or sets the current volume of the voice read from the microphone.
        /// </summary>
        public int MicrophoneVoiceVolume {
            get => this.microphoneMicrophoneVoiceVolume;
            set => this.RaiseAndSetIfChanged(ref this.microphoneMicrophoneVoiceVolume, value);
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

                this.RaiseAndSetIfChanged(ref this.pauseThreshold, value);
                this.MicrophoneThresholdVisualizationMargin = new Thickness(value, this.MicrophoneThresholdVisualizationMargin.Top, this.MicrophoneThresholdVisualizationMargin.Right, this.MicrophoneThresholdVisualizationMargin.Bottom);
            }
        }

        /// <summary>
        ///     Gets or sets the string representation of the microphone device name from NAudio.
        /// </summary>
        public int SelectedMicrophone {
            get => this.selectedMicrophone;
            set => this.RaiseAndSetIfChanged(ref this.selectedMicrophone, value);
        }

        /// <summary>
        ///     Raised when properties are changed on the object.
        /// </summary>
        /// <param name="sender">The object invoked on.</param>
        /// <param name="e">The property changed information.</param>
        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) {
            if (nameof(this.MicrophoneVoiceVolume).Equals(e.PropertyName, StringComparison.InvariantCultureIgnoreCase)) {
                return;
            }

            if (nameof(this.SelectedMicrophone).Equals(e.PropertyName, StringComparison.InvariantCultureIgnoreCase)) {
                this.pausingObject.SelectedMicrophone = this.SelectedMicrophone;
                this.pausingObject.StopListenToMicrophone();
                this.pausingObject.StartListenToMicrophone();
            }

            this.config.MicrophoneGuid = NAudioUtilities.GetInputDevice(this.SelectedMicrophone - 1).ProductGuid.ToString();
            this.config.PauseThreshold = this.PauseThreshold;
            this.config.WriteConfiguration();
        }

        /// <summary>
        ///     Raised when properties change on the <see cref="pausingObject" />.
        /// </summary>
        /// <param name="sender">The <see cref="pausingObject" />.</param>
        /// <param name="e">The property changed information.</param>
        private void Pauser_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
            if (!nameof(this.pausingObject.MicrophoneVoiceVolume).Equals(e.PropertyName, StringComparison.InvariantCultureIgnoreCase)) {
                return;
            }

            this.MicrophoneVoiceVolume = this.pausingObject.MicrophoneVoiceVolume;
        }
    }
}