namespace streaming_tools.Utilities {
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using PInvoke;

    /// <summary>
    ///     Helpers for dealing with low level calls to the OS' monitor interface.
    /// </summary>
    internal static class MonitorUtilities {
        /// <summary>
        ///     The default name to return when a monitor isn't found.
        /// </summary>
        public const string MONITOR_NOT_FOUND_DEVICE_NAME = "MONITOR_NOT_FOUND";

        /// <summary>
        ///     The default struct to return when a monitor isn't found.
        /// </summary>
        public static readonly PInvokeUtilities.MonitorInfoEx MONITOR_NOT_FOUND = new() { DeviceName = MonitorUtilities.MONITOR_NOT_FOUND_DEVICE_NAME };

        /// <summary>
        ///     Gets a collection representing all monitors on the machine.
        /// </summary>
        /// <returns>A collection of all monitors if successful, an empty collection otherwise.</returns>
        public static IList<PInvokeUtilities.MonitorInfoEx> GetMonitors() {
            // The returnable collection.
            var monitors = new List<PInvokeUtilities.MonitorInfoEx>();

            // Use a low-level PInvoke to iterate though the monitors.
            // ReSharper disable once RedundantUnsafeContext
            unsafe {
                User32.EnumDisplayMonitors(
                    IntPtr.Zero,
                    IntPtr.Zero,
                    (monitor, a, b, c) => {
                        var mi = new PInvokeUtilities.MonitorInfoEx();
                        mi.Size = Marshal.SizeOf(mi);
                        var success = PInvokeUtilities.GetMonitorInfo(monitor, ref mi);
                        if (success) {
                            monitors.Add(mi);
                        }

                        return true;
                    },
                    IntPtr.Zero);
            }

            // Return what we found.
            return monitors;
        }

        /// <summary>
        ///     Gets the primary monitor.
        /// </summary>
        /// <returns>The primary monitor if found, <see cref="MONITOR_NOT_FOUND" /> otherwise.</returns>
        public static PInvokeUtilities.MonitorInfoEx GetPrimaryMonitor() {
            // Try to find the primary monitor using the primary monitor flag.
            foreach (var monitor in MonitorUtilities.GetMonitors()) {
                if (((User32.MONITORINFO_Flags)monitor.Flags).HasFlag(User32.MONITORINFO_Flags.MONITORINFOF_PRIMARY)) {
                    return monitor;
                }
            }

            return MonitorUtilities.MONITOR_NOT_FOUND;
        }
    }
}