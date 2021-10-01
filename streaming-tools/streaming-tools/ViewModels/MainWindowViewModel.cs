namespace streaming_tools.ViewModels {
    using ReactiveUI;

    /// <summary>
    ///     The business logic behind the main UI.
    /// </summary>
    public class MainWindowViewModel : ViewModelBase {
        /// <summary>
        ///     The view responsible for specifying twitch accounts.
        /// </summary>
        private AccountsViewModel accountsViewModel;

        /// <summary>
        ///     The view responsible for managing the sounds played for channel point redemptions.
        /// </summary>
        private ChannelPointViewModel channelPointViewModel;

        /// <summary>
        ///     The view responsible for managing the keystroke command.
        /// </summary>
        private KeystrokeCommandViewModel keystrokeCommandViewModel;

        /// <summary>
        ///     The view responsible for laying out windows on the OS.
        /// </summary>
        private LayoutsViewModel layoutViewModel;

        /// <summary>
        ///     The view responsible for pausing TTS when the microphone hears things.
        /// </summary>
        private TtsPauseConfigViewModel ttsPauseConfigViewModel;

        /// <summary>
        ///     The view model for the phonetic words list.
        /// </summary>
        private TtsPhoneticWordsViewModel ttsPhoneticWordsViewModel;

        /// <summary>
        ///     The view responsible for managing the list of usernames to skip.
        /// </summary>
        private TtsSkipUsernamesViewModel ttsSkipUsernamesViewModel;

        /// <summary>
        ///     The view responsible for holding the configurations for each twitch chat connection.
        /// </summary>
        private TwitchChatConfigsViewModel twitchChatConfigs;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MainWindowViewModel" /> class.
        /// </summary>
#pragma warning disable 8618
        public MainWindowViewModel() {
#pragma warning restore 8618
            this.AccountsViewModel = new AccountsViewModel();
            this.ChannelPointViewModel = new ChannelPointViewModel();
            this.KeystrokeCommandViewModel = new KeystrokeCommandViewModel();
            this.LayoutViewModel = new LayoutsViewModel();
            this.TtsPauseConfigViewModel = new TtsPauseConfigViewModel();
            this.TtsPhoneticWordsViewModel = new TtsPhoneticWordsViewModel();
            this.TtsSkipUsernamesViewModel = new TtsSkipUsernamesViewModel();
            this.TwitchChatConfigs = new TwitchChatConfigsViewModel();
        }

        /// <summary>
        ///     Gets or sets the view responsible for specifying twitch accounts.
        /// </summary>
        public AccountsViewModel AccountsViewModel {
            get => this.accountsViewModel;
            set => this.RaiseAndSetIfChanged(ref this.accountsViewModel, value);
        }

        /// <summary>
        ///     Gets or sets the  view responsible for managing the sounds played for channel point redemptions.
        /// </summary>
        public ChannelPointViewModel ChannelPointViewModel {
            get => this.channelPointViewModel;
            set => this.RaiseAndSetIfChanged(ref this.channelPointViewModel, value);
        }

        /// <summary>
        ///     Gets or sets the  view responsible for managing the keystroke command.
        /// </summary>
        public KeystrokeCommandViewModel KeystrokeCommandViewModel {
            get => this.keystrokeCommandViewModel;
            set => this.RaiseAndSetIfChanged(ref this.keystrokeCommandViewModel, value);
        }

        /// <summary>
        ///     Gets or sets the view responsible for laying out windows on the OS.
        /// </summary>
        public LayoutsViewModel LayoutViewModel {
            get => this.layoutViewModel;
            set => this.RaiseAndSetIfChanged(ref this.layoutViewModel, value);
        }

        /// <summary>
        ///     Gets or sets the  view responsible for pausing TTS when the microphone hears things.
        /// </summary>
        public TtsPauseConfigViewModel TtsPauseConfigViewModel {
            get => this.ttsPauseConfigViewModel;
            set => this.RaiseAndSetIfChanged(ref this.ttsPauseConfigViewModel, value);
        }

        /// <summary>
        ///     Gets or sets the  view model for the phonetic words list.
        /// </summary>
        public TtsPhoneticWordsViewModel TtsPhoneticWordsViewModel {
            get => this.ttsPhoneticWordsViewModel;
            set => this.RaiseAndSetIfChanged(ref this.ttsPhoneticWordsViewModel, value);
        }

        /// <summary>
        ///     Gets or sets the  view responsible for managing the list of usernames to skip.
        /// </summary>
        public TtsSkipUsernamesViewModel TtsSkipUsernamesViewModel {
            get => this.ttsSkipUsernamesViewModel;
            set => this.RaiseAndSetIfChanged(ref this.ttsSkipUsernamesViewModel, value);
        }

        /// <summary>
        ///     Gets or sets the  view responsible for holding the configurations for each twitch chat connection.
        /// </summary>
        public TwitchChatConfigsViewModel TwitchChatConfigs {
            get => this.twitchChatConfigs;
            set => this.RaiseAndSetIfChanged(ref this.twitchChatConfigs, value);
        }
    }
}