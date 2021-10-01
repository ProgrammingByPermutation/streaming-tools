namespace streaming_tools.Views {
    using Avalonia.Controls;
    using Avalonia.Markup.Xaml;

    /// <summary>
    ///     The view responsible for mapping channel point redemptions to sounds.
    /// </summary>
    public class ChannelPointView : UserControl {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ChannelPointView" /> class.
        /// </summary>
        public ChannelPointView() {
            this.InitializeComponent();
        }

        /// <summary>
        ///     Initializes the UI components.
        /// </summary>
        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }
    }
}