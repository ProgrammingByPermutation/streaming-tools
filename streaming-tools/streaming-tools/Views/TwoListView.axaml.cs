namespace streaming_tools.Views {
    using Avalonia.Controls;
    using Avalonia.Interactivity;
    using Avalonia.Markup.Xaml;
    using ViewModels;

    /// <summary>
    ///     A control that handles maintaining two lists and moving items between them.
    /// </summary>
    public class TwoListView : UserControl {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TwoListView" /> class.
        /// </summary>
        public TwoListView() {
            this.InitializeComponent();
        }

        /// <summary>
        ///     Initializes the GUI components.
        /// </summary>
        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        ///     Raised when an item in the left list is double clicked.
        /// </summary>
        /// <param name="sender">The list view.</param>
        /// <param name="e">The event arguments.</param>
        private void Left_OnDoubleTapped(object? sender, RoutedEventArgs e) {
            (this.DataContext as TwoListViewModel)?.OnLeftDoubleClick?.Invoke((sender as ListBox)?.SelectedItem as string);
        }

        /// <summary>
        ///     Raised when an item in the right list is double clicked.
        /// </summary>
        /// <param name="sender">The list view.</param>
        /// <param name="e">The event arguments.</param>
        private void Right_OnDoubleTapped(object? sender, RoutedEventArgs e) {
            (this.DataContext as TwoListViewModel)?.OnRightDoubleClick?.Invoke((sender as ListBox)?.SelectedItem as string);
        }
    }
}