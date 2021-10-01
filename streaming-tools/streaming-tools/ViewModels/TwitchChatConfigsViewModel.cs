namespace streaming_tools.ViewModels {
    using System.Collections.ObjectModel;
    using ReactiveUI;
    using Views;

    /// <summary>
    ///     The view responsible for keeping track of the configuration to each twitch chat.
    /// </summary>
    public class TwitchChatConfigsViewModel : ViewModelBase {
        /// <summary>
        ///     The singleton collection for configuring the application.
        /// </summary>
        private readonly Configuration config;

        /// <summary>
        ///     The collection of all twitch chat configurations.
        /// </summary>
        private ObservableCollection<TwitchChatConfigView> twitchChatConfigs = new();

        /// <summary>
        ///     Initializes a new instance of the <see cref="TwitchChatConfigsViewModel" /> class.
        /// </summary>
        public TwitchChatConfigsViewModel() {
            this.config = Configuration.Instance;

            if (null == this.config.TwitchChatConfigs) {
                return;
            }

            foreach (var twitchConfig in this.config.TwitchChatConfigs) {
                this.CreateTwitchChatConfig(twitchConfig);
            }
        }

        /// <summary>
        ///     Gets or sets the collection of all twitch chat configurations.
        /// </summary>
        public ObservableCollection<TwitchChatConfigView> TwitchChatConfigs {
            get => this.twitchChatConfigs;
            set => this.RaiseAndSetIfChanged(ref this.twitchChatConfigs, value);
        }

        /// <summary>
        ///     Creates a new configuration object.
        /// </summary>
        public void AddConfigCommand() {
            this.CreateTwitchChatConfig(null);
        }

        /// <summary>
        ///     Creates a new configuration object.
        /// </summary>
        /// <param name="twitchConfig">The twitch chat configuration to base the config on.</param>
        public void CreateTwitchChatConfig(TwitchChatConfiguration? twitchConfig) {
            if (null == this.config.TwitchChatConfigs) {
                return;
            }

            if (null == twitchConfig) {
                twitchConfig = new TwitchChatConfiguration();
                this.config.TwitchChatConfigs.Add(twitchConfig);
                this.config.WriteConfiguration();
            }

            TwitchChatConfigView view = new();
            TwitchChatConfigViewModel viewModel = new(twitchConfig);
            viewModel.DeleteConfig = () => {
                this.TwitchChatConfigs.Remove(view);
                viewModel.Dispose();
                this.config.TwitchChatConfigs.Remove(twitchConfig);
                this.config.WriteConfiguration();
            };
            view.DataContext = viewModel;

            this.TwitchChatConfigs.Add(view);
        }
    }
}