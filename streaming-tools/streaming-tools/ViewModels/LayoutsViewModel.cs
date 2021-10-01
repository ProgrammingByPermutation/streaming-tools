namespace streaming_tools.ViewModels {
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using PInvoke;
    using ReactiveUI;
    using Utilities;

    /// <summary>
    ///     The view model for handling laying out windows on the OS.
    /// </summary>
    public class LayoutsViewModel : ViewModelBase {
        /// <summary>
        ///     The amount of padding to add to the width.
        ///     TODO: Make this configurable in the app
        /// </summary>
        private const int PADDING = -15;

        /// <summary>
        ///     The previous window settings before we touched them.
        /// </summary>
        private readonly Dictionary<int, User32.SetWindowLongFlags> previousWindowSettings = new();

        /// <summary>
        ///     The currently selected monitor to move the windows to.
        /// </summary>
        private string? selectedMonitor;

        /// <summary>
        ///     Initializes a new instance of the <see cref="LayoutsViewModel" /> class.
        /// </summary>
        public LayoutsViewModel() {
            var primaryMonitor = MonitorUtilities.GetPrimaryMonitor();

            if (string.IsNullOrWhiteSpace(Configuration.Instance.LayoutSelectedMonitor)) {
                this.SelectedMonitor = MonitorUtilities.MONITOR_NOT_FOUND_DEVICE_NAME != primaryMonitor.DeviceName ? primaryMonitor.DeviceName : MonitorUtilities.GetMonitors().FirstOrDefault().DeviceName;
            } else {
                this.SelectedMonitor = Configuration.Instance.LayoutSelectedMonitor;
            }

            this.PropertyChanged += this.OnPropertyChanged;
        }

        /// <summary>
        ///     Gets or sets the currently selected monitor to move the windows to.
        /// </summary>
        public string? SelectedMonitor {
            get => this.selectedMonitor;
            set => this.RaiseAndSetIfChanged(ref this.selectedMonitor, value);
        }

        /// <summary>
        ///     Handles rearranging the windows.
        /// </summary>
        public void OnExecuteClicked() {
            // Get the monitors
            var monitors = MonitorUtilities.GetMonitors();
            var processes = Process.GetProcessesByName("vlc").ToList();
            if (0 == processes.Count) {
                return;
            }

            // Trim minimized applications
            for (var i = processes.Count - 1; i >= 0; i--) {
                if (User32.IsIconic(processes[i].MainWindowHandle)) {
                    processes.RemoveAt(i);
                }
            }

            // Determine the size of the windows when tiled based on the total area of
            // the monitor
            var monitor = null == this.SelectedMonitor ? MonitorUtilities.GetPrimaryMonitor() : monitors.FirstOrDefault(m => this.SelectedMonitor.Equals(m.DeviceName, StringComparison.InvariantCultureIgnoreCase));
            var monitorWidth = monitor.WorkArea.Right - monitor.WorkArea.Left;
            var monitorHeight = monitor.WorkArea.Bottom - monitor.WorkArea.Top;
            var width = 2 <= processes.Count ? (int)Math.Ceiling(monitorWidth / 2.0f) : monitorWidth;
            width += (int)(LayoutsViewModel.PADDING / 2.0 * -1.0);
            var height = monitorHeight;
            var rows = Math.Ceiling(processes.Count / 2.0f);
            height = (int)Math.Ceiling(height / rows);

            // Apply the layout
            for (var i = 0; i < processes.Count; i++) {
                var process = processes[i];
                var row = (int)Math.Floor(i / 2.0);
                var column = i % 2 == 0 ? 0 : 1;
                var x = monitor.WorkArea.Left + column * width + (column == 1 ? LayoutsViewModel.PADDING : 0);
                var y = monitor.WorkArea.Top + row * height;

                if (!this.previousWindowSettings.ContainsKey(process.Id)) {
                    this.previousWindowSettings[process.Id] = (User32.SetWindowLongFlags)User32.GetWindowLong(process.MainWindowHandle, User32.WindowLongIndexFlags.GWL_STYLE);
                }

                User32.SetWindowLong(process.MainWindowHandle, User32.WindowLongIndexFlags.GWL_STYLE, User32.SetWindowLongFlags.WS_VISIBLE);
                User32.SetWindowPos(process.MainWindowHandle, User32.SpecialWindowHandles.HWND_TOP, x, y, width, height, User32.SetWindowPosFlags.SWP_SHOWWINDOW);
                User32.SetForegroundWindow(process.MainWindowHandle);
            }
        }

        /// <summary>
        ///     Handles rolling back the rearranging of windows.
        /// </summary>
        public void OnUndoClicked() {
            foreach (var process in Process.GetProcessesByName("vlc")) {
                if (!this.previousWindowSettings.TryGetValue(process.Id, out var oldValue)) {
                    continue;
                }

                User32.SetWindowLong(process.MainWindowHandle, User32.WindowLongIndexFlags.GWL_STYLE, oldValue);
            }
        }

        /// <summary>
        ///     Raised when properties are changed to update the configuration file.
        /// </summary>
        /// <param name="sender">This class' object.</param>
        /// <param name="e">The event arguments.</param>
        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) {
            Configuration.Instance.LayoutSelectedMonitor = this.SelectedMonitor;
            Configuration.Instance.WriteConfiguration();
        }
    }
}