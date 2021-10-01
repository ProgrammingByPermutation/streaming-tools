namespace streaming_tools.Views {
    using System.Linq;
    using Avalonia.Controls;
    using Avalonia.Markup.Xaml;
    using Utilities;

    /// <summary>
    ///     The UI for laying out windows on the OS.
    /// </summary>
    public class Layouts : UserControl {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Layouts" /> class.
        /// </summary>
        public Layouts() {
            this.InitializeComponent();
        }

        /// <summary>
        ///     Handles getting the list of monitors for the combo box.
        /// </summary>
        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            // Setup the list of monitors
            var monitors = this.Find<ComboBox>("monitors");
            var monitorsFound = MonitorUtilities.GetMonitors();
            var monitorItems = Enumerable.Range(0, monitorsFound.Count).Select(n => monitorsFound[n].DeviceName).ToArray();
            monitors.Items = monitorItems;
        }
    }
}