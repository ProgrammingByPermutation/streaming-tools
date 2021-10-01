namespace streaming_tools.ViewModels {
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using Avalonia.Controls;
    using DynamicData;
    using ReactiveUI;
    using Twitch;
    using TwitchLib.PubSub.Events;
    using Utilities;

    /// <summary>
    ///     The view responsible for mapping channel point redemptions to sounds.
    /// </summary>
    public class ChannelPointViewModel : ViewModelBase {
        /// <summary>
        ///     The list of channel point redemption sounds.
        /// </summary>
        private ObservableCollection<ChannelPointSoundWrapper> channelPointSounds;

        /// <summary>
        ///     The master volume to scale all channel point sounds at.
        /// </summary>
        private uint masterVolume;

        /// <summary>
        ///     The output device to send audio to.
        /// </summary>
        private string? outputDevice;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChannelPointViewModel" /> class.
        /// </summary>
        public ChannelPointViewModel() {
            this.channelPointSounds = new ObservableCollection<ChannelPointSoundWrapper>();
            this.OutputDevice = Configuration.Instance.ChannelPointSoundRedemptionOutputChannel;
            this.MasterVolume = Configuration.Instance.ChannelPointSoundRedemptionsMasterVolume ?? 50;

            // Create the list of Output devices.
            var outputDevices = NAudioUtilities.GetTotalOutputDevices();
            this.OutputDevices = new ObservableCollection<string>();
            this.OutputDevices.AddRange(Enumerable.Range(-1, outputDevices + 1).Select(n => NAudioUtilities.GetOutputDevice(n).ProductName));

            this.RefreshChannelPointRewards();
            this.PropertyChanged += this.PropertyChangedHandler;
        }

        /// <summary>
        ///     Gets or sets the collection of channel point sounds.
        /// </summary>
        public ObservableCollection<ChannelPointSoundWrapper> ChannelPointSounds {
            get => this.channelPointSounds;
            set => this.RaiseAndSetIfChanged(ref this.channelPointSounds, value);
        }

        /// <summary>
        ///     Gets or sets the master volume to scale all channel point sounds at.
        /// </summary>
        public uint MasterVolume {
            get => this.masterVolume;
            set => this.RaiseAndSetIfChanged(ref this.masterVolume, value);
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
        ///     Refreshes the full list of existing channel point redemption sounds.
        /// </summary>
        public async void RefreshChannelPointRewards() {
            try {
                this.ChannelPointSounds.Clear();
                if (null == Configuration.Instance.ChannelPointSoundRedemptions) {
                    return;
                }

                var account = Configuration.Instance.TwitchAccounts?.FirstOrDefault(a => a.IsUsersStreamingAccount);
                if (null == account || null == account.Username) {
                    return;
                }

                var api = await TwitchChatManager.Instance.GetTwitchClientApi(account.Username);
                if (null == api) {
                    return;
                }

                var response = await api.Helix.Users.GetUsersAsync(logins: new List<string> { account.Username });
                var user = response.Users.FirstOrDefault();
                if (null == user) {
                    return;
                }

                var rewards = await api.Helix.ChannelPoints.GetCustomReward(user.Id);
                var actualRewardNames = rewards.Data.Select(r => r.Title);
                var knownRewardNames = Configuration.Instance.ChannelPointSoundRedemptions.Select(r => r.Name);
                var toAdd = actualRewardNames.Except(knownRewardNames);
                var toDelete = knownRewardNames.Except(actualRewardNames);

                foreach (var reward in toAdd) {
                    Configuration.Instance.ChannelPointSoundRedemptions.Add(new ChannelPointSoundRedemption { Name = reward });
                }

                foreach (var reward in toDelete) {
                    var found = Configuration.Instance.ChannelPointSoundRedemptions.FirstOrDefault(c => c.Name?.Equals(reward, StringComparison.InvariantCultureIgnoreCase) == true);
                    if (null != found) {
                        Configuration.Instance.ChannelPointSoundRedemptions.Remove(found);
                    }
                }

                this.ChannelPointSounds.AddRange(Configuration.Instance.ChannelPointSoundRedemptions.Select(c => new ChannelPointSoundWrapper(this, c)));
                TwitchChatManager.Instance.AddChannelPointsCallback(account, account.Username, this.OnRewardRedeemed);
            } catch (Exception) { }
        }

        /// <summary>
        ///     Raised when a channel point is redeemed.
        /// </summary>
        /// <param name="sender">The pub sub object.</param>
        /// <param name="e">The reward information.</param>
        private void OnRewardRedeemed(object? sender, OnRewardRedeemedArgs e) {
            if (null == Configuration.Instance.ChannelPointSoundRedemptions || string.IsNullOrWhiteSpace(this.OutputDevice)) {
                return;
            }

            var reward = Configuration.Instance.ChannelPointSoundRedemptions.FirstOrDefault(c => c.Name?.Equals(e.RewardTitle, StringComparison.InvariantCultureIgnoreCase) == true);
            if (null == reward || string.IsNullOrWhiteSpace(reward.Filename) || !File.Exists(reward.Filename)) {
                return;
            }

            double volume = reward.Volume ?? 100;
            if (null != Configuration.Instance.ChannelPointSoundRedemptionsMasterVolume && 0 != Configuration.Instance.ChannelPointSoundRedemptionsMasterVolume.Value) {
                volume *= Configuration.Instance.ChannelPointSoundRedemptionsMasterVolume.Value / 100.0;
            }

            if (volume < 0) {
                volume = 0;
            }

            GlobalSoundManager.Instance.QueueSound(reward.Filename, this.OutputDevice, (int)volume);
        }

        /// <summary>
        ///     Raised to keep the configuration up-to-date with the user settings.
        /// </summary>
        /// <param name="sender">This object.</param>
        /// <param name="e">The information on the property that changed.</param>
        private void PropertyChangedHandler(object? sender, PropertyChangedEventArgs e) {
            if (nameof(this.OutputDevice).Equals(e.PropertyName, StringComparison.InvariantCultureIgnoreCase)) {
                Configuration.Instance.ChannelPointSoundRedemptionOutputChannel = this.OutputDevice;
                Configuration.Instance.WriteConfiguration();
            } else if (nameof(this.MasterVolume).Equals(e.PropertyName, StringComparison.InvariantCultureIgnoreCase)) {
                Configuration.Instance.ChannelPointSoundRedemptionsMasterVolume = this.MasterVolume;
                Configuration.Instance.WriteConfiguration();
            }
        }

        /// <summary>
        ///     The wrapper that maps channel points to their sounds.
        /// </summary>
        public class ChannelPointSoundWrapper : ReactiveObject {
            /// <summary>
            ///     The reference to the view model.
            /// </summary>
            private readonly ChannelPointViewModel parent;

            /// <summary>
            ///     The reference to the persistent configuration object.
            /// </summary>
            private readonly ChannelPointSoundRedemption source;

            /// <summary>
            ///     The filename of the sound file.
            /// </summary>
            private string? filename;

            /// <summary>
            ///     The name of the channel point redemption.
            /// </summary>
            private string? name;

            /// <summary>
            ///     The volume to play the sound at.
            /// </summary>
            private int volume;

            /// <summary>
            ///     Initializes a new instance of the <see cref="ChannelPointSoundWrapper" /> class.
            /// </summary>
            /// <param name="parent">The reference to the view model.</param>
            /// <param name="source">The persistent configuration object.</param>
            public ChannelPointSoundWrapper(ChannelPointViewModel parent, ChannelPointSoundRedemption source) {
                this.parent = parent;
                this.source = source;
                this.Name = source.Name;
                this.Filename = source.Filename;
                this.Volume = source.Volume ?? 100;
                this.PropertyChanged += this.PropertyChangedHandler;
            }

            /// <summary>
            ///     Gets or sets the filename of the sound file.
            /// </summary>
            public string? Filename {
                get => this.filename;
                set => this.RaiseAndSetIfChanged(ref this.filename, value);
            }

            /// <summary>
            ///     Gets or sets the name of the channel point redemption.
            /// </summary>
            public string? Name {
                get => this.name;
                set => this.RaiseAndSetIfChanged(ref this.name, value);
            }

            /// <summary>
            ///     Gets or sets the volume to play the sound at.
            /// </summary>
            public int Volume {
                get => this.volume;
                set => this.RaiseAndSetIfChanged(ref this.volume, value);
            }

            /// <summary>
            ///     Opens the file dialog and gets the response from the user.
            /// </summary>
            public async void OpenFileDialog() {
                var dlg = new OpenFileDialog();
                dlg.AllowMultiple = false;
                dlg.Title = "Select a Sound File";
                var filenames = await dlg.ShowAsync(Constants.MAIN_WINDOW);
                if (null == filenames) {
                    return;
                }

                this.Filename = filenames.FirstOrDefault();
            }

            /// <summary>
            ///     Previews playing the supplied file.
            /// </summary>
            public void PlayPreview() {
                if (string.IsNullOrWhiteSpace(this.Filename) || !File.Exists(this.Filename) || string.IsNullOrWhiteSpace(this.parent.OutputDevice)) {
                    return;
                }

                double volume = this.Volume;
                if (null != Configuration.Instance.ChannelPointSoundRedemptionsMasterVolume && 0 != Configuration.Instance.ChannelPointSoundRedemptionsMasterVolume.Value) {
                    volume *= Configuration.Instance.ChannelPointSoundRedemptionsMasterVolume.Value / 100.0;
                }

                if (volume < 0) {
                    volume = 0;
                }

                GlobalSoundManager.Instance.QueueSound(this.Filename, this.parent.OutputDevice, (int)volume);
            }

            /// <summary>
            ///     Raised when properties change on the class to write them to the configuration.
            /// </summary>
            /// <param name="sender">This object.</param>
            /// <param name="e">The information on the property that changed.</param>
            private void PropertyChangedHandler(object? sender, PropertyChangedEventArgs e) {
                if (nameof(this.Name).Equals(e.PropertyName, StringComparison.InvariantCultureIgnoreCase)) {
                    this.source.Name = this.Name;
                } else if (nameof(this.Filename).Equals(e.PropertyName, StringComparison.InvariantCultureIgnoreCase)) {
                    this.source.Filename = this.Filename;
                } else if (nameof(this.Volume).Equals(e.PropertyName, StringComparison.InvariantCultureIgnoreCase)) {
                    this.source.Volume = this.Volume;
                }

                Configuration.Instance.WriteConfiguration();
            }
        }
    }
}