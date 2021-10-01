namespace streaming_tools.Views {
    using Avalonia.Controls;
    using Avalonia.Markup.Xaml;

    /// <summary>
    ///     The view responsible for keeping track of the configuration to each twitch chat.
    /// </summary>
    public class TwitchChatConfigsView : UserControl {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TwitchChatConfigsView" /> class.
        /// </summary>
        public TwitchChatConfigsView() {
            this.InitializeComponent();
        }

        /// <summary>
        ///     Creates the UI components.
        /// </summary>
        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }
    }
}