namespace streaming_tools.Views {
    using Avalonia.Controls;
    using Avalonia.Markup.Xaml;

    /// <summary>
    ///     The view responsible for assigning a command to a keystroke.
    /// </summary>
    public class KeystrokeCommandView : UserControl {
        /// <summary>
        ///     Initializes a new instance of the <see cref="KeystrokeCommandView" /> class.
        /// </summary>
        public KeystrokeCommandView() {
            this.InitializeComponent();
        }

        /// <summary>
        ///     The initializes the UI components.
        /// </summary>
        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }
    }
}