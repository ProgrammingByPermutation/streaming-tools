namespace streaming_tools.ViewModels {
    using System;
    using System.ComponentModel;
    using System.Linq;
    using Avalonia.Controls;
    using DynamicData;
    using ReactiveUI;
    using Twitch.Admin;
    using Views;

    /// <summary>
    ///     The view model for configuring the various settings for admin filters.
    /// </summary>
    public class AdminFiltersViewModel : ViewModelBase {
        /// <summary>
        ///     The persistent twitch configuration.
        /// </summary>
        private readonly TwitchChatConfiguration config;

        /// <summary>
        ///     True if bots from known bot lists should be banned, false otherwise.
        /// </summary>
        private bool banBotsFromKnownList;

        /// <summary>
        ///     True if people that follow too quickly should be considered bots and banned, false otherwise.
        /// </summary>
        private bool banFollowsTooQuick;

        /// <summary>
        ///     True if the logic for scanning followers for bots is currently running, false otherwise.
        /// </summary>
        private bool lookupBotsInFollowerListRunning;

        /// <summary>
        ///     The username to lookup bots for.
        /// </summary>
        private string? lookupBotsInFollowerListUser;

        /// <summary>
        ///     A value indicating whether we should timeout non-ASCII messages.
        /// </summary>
        private bool timeoutNonAscii;

        /// <summary>
        ///     The title of the dialog.
        /// </summary>
        private string title;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AdminFiltersViewModel" /> class.
        /// </summary>
        public AdminFiltersViewModel() {
            // This is for the designer.
            this.config = new TwitchChatConfiguration();
            this.title = "Admin Filters";
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AdminFiltersViewModel" /> class.
        /// </summary>
        /// <param name="config">The twitch configuration.</param>
        public AdminFiltersViewModel(TwitchChatConfiguration config) : this() {
            this.config = config;
            this.title = $"Admin Filters: {config.TwitchChannel}";

            this.LookupBotsInFollowerListUser = this.config.TwitchChannel;
            this.PropertyChanged += this.OnPropertyChanged;
        }

        /// <summary>
        ///     Gets or sets the title of the window.
        /// </summary>
        public string Title {
            get => this.title;
            set => this.RaiseAndSetIfChanged(ref this.title, value);
        }

        /// <summary>
        ///     Gets or sets the username to lookup follow bots for.
        /// </summary>
        public string? LookupBotsInFollowerListUser {
            get => this.lookupBotsInFollowerListUser;
            set => this.RaiseAndSetIfChanged(ref this.lookupBotsInFollowerListUser, value);
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the logic for scanning followers for bots is currently running.
        /// </summary>
        public bool LookupBotsInFollowerListRunning {
            get => this.lookupBotsInFollowerListRunning;
            set => this.RaiseAndSetIfChanged(ref this.lookupBotsInFollowerListRunning, value);
        }

        /// <summary>
        ///     Gets the delegate that closes the window.
        /// </summary>
        public Action<Window> CloseWindow {
            get { return window => { window.Close(); }; }
        }

        /// <summary>
        ///     Handles updating the persistent configuration with the changes to the UI.
        /// </summary>
        /// <param name="sender">The property.</param>
        /// <param name="e">The event arguments.</param>
        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        }
    }
}