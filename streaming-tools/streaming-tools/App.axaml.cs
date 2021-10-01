namespace streaming_tools {
    using Avalonia;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Markup.Xaml;
    using ViewModels;
    using Views;

    /// <summary>
    ///     The main entry point of the application.
    /// </summary>
    public class App : Application {
        /// <summary>
        ///     Creates the UI components.
        /// </summary>
        public override void Initialize() {
            AvaloniaXamlLoader.Load(this);
            Constants.CLIPBOARD = this.Clipboard;
        }

        /// <summary>
        ///     The initialization method for performing one time initialization for the application.
        /// </summary>
        public override void OnFrameworkInitializationCompleted() {
            if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                desktop.MainWindow = new MainWindow { DataContext = new MainWindowViewModel() };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}