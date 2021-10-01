namespace streaming_tools.Views {
    using Avalonia.Controls;
    using Avalonia.Markup.Xaml;

    /// <summary>
    ///     The view responsible for pausing twitch chat TTS when the microphone hears things.
    /// </summary>
    public class TtsPauseConfigView : UserControl {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TtsPauseConfigView" /> class.
        /// </summary>
        public TtsPauseConfigView() {
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