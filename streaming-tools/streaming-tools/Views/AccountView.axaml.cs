namespace streaming_tools.Views {
    using Avalonia.Controls;
    using Avalonia.Markup.Xaml;

    /// <summary>
    ///     The UI representation of a twitch account.
    /// </summary>
    public class AccountView : UserControl {
        /// <summary>
        ///     Initializes a new instance of the <see cref="AccountView" /> class.
        /// </summary>
        public AccountView() {
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