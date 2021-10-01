namespace streaming_tools.Views {
    using Avalonia.Controls;
    using Avalonia.Markup.Xaml;

    /// <summary>
    ///     Handles updating the list and credentials for twitch accounts.
    /// </summary>
    public class AccountsView : UserControl {
        /// <summary>
        ///     Initializes a new instance of the <see cref="AccountsView" /> class.
        /// </summary>
        public AccountsView() {
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