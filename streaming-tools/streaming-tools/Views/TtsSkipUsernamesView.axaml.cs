namespace streaming_tools.Views {
    using Avalonia.Controls;
    using Avalonia.Markup.Xaml;

    /// <summary>
    ///     A view for managing the list of usernames to skip in TTS.
    /// </summary>
    public class TtsSkipUsernamesView : UserControl {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TtsSkipUsernamesView" /> class.
        /// </summary>
        public TtsSkipUsernamesView() {
            this.InitializeComponent();
        }

        /// <summary>
        ///     Initializes the GUI components.
        /// </summary>
        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }
    }
}