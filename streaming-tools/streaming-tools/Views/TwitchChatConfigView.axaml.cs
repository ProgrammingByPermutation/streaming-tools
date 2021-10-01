namespace streaming_tools.Views {
    using Avalonia.Controls;
    using Avalonia.Markup.Xaml;

    /// <summary>
    ///     The view responsible for a single entry in the twitch chat configuration list.
    /// </summary>
    public class TwitchChatConfigView : UserControl {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TwitchChatConfigView" /> class.
        /// </summary>
        public TwitchChatConfigView() {
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