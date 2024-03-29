﻿// <auto-generated/>
#pragma warning disable CS1658
#pragma warning disable CS1574
#pragma warning disable CS1584
#pragma warning disable CS1580

namespace streaming_tools.Utilities {
    using System;
    using System.Runtime.InteropServices;

    internal class PInvokeUtilities {
        /// <summary>
        ///     size of a device name string
        /// </summary>
        private const int CCHDEVICENAME = 32;

        /// <summary>
        ///     Retrieves information about a display monitor.
        /// </summary>
        /// <param name="hMonitor">A handle to the display monitor of interest.</param>
        /// <param name="lpmi">
        ///     A pointer to a <see cref="MONITORINFO" /> or <see cref="MONITORINFOEX" /> structure that receives information about
        ///     the specified display monitor.
        ///     You must set the cbSize member of the structure to <c>sizeof(MONITORINFO)</c> or <c>sizeof(MONITORINFOEX)</c>
        ///     before calling the
        ///     <see cref="GetMonitorInfo(IntPtr, MONITORINFO*)" /> function. Doing so lets the function determine the type of
        ///     structure you are passing to it.
        ///     The <see cref="MONITORINFOEX" /> structure is a superset of the <see cref="MONITORINFO" /> structure. It has one
        ///     additional member: a
        ///     string that contains a name for the display monitor. Most applications have no use for a display monitor name, and
        ///     so can save some
        ///     bytes by using a <see cref="MONITORINFO" /> structure.
        /// </param>
        /// <returns>
        ///     If the function succeeds, the return value is <c>true</c>.
        ///     If the function fails, the return value is <c>false</c>.
        /// </returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

        /// <summary>
        ///     The MONITORINFOEX structure contains information about a display monitor.
        ///     The GetMonitorInfo function stores information into a MONITORINFOEX structure or a MONITORINFO structure.
        ///     The MONITORINFOEX structure is a superset of the MONITORINFO structure. The MONITORINFOEX structure adds a string
        ///     member to contain a name
        ///     for the display monitor.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MonitorInfoEx {
            /// <summary>
            ///     The size, in bytes, of the structure. Set this member to sizeof(MONITORINFOEX) (72) before calling the
            ///     GetMonitorInfo function.
            ///     Doing so lets the function determine the type of structure you are passing to it.
            /// </summary>
            public int Size;

            /// <summary>
            ///     A RECT structure that specifies the display monitor rectangle, expressed in virtual-screen coordinates.
            ///     Note that if the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative
            ///     values.
            /// </summary>
            public RectStruct Monitor;

            /// <summary>
            ///     A RECT structure that specifies the work area rectangle of the display monitor that can be used by applications,
            ///     expressed in virtual-screen coordinates. Windows uses this rectangle to maximize an application on the monitor.
            ///     The rest of the area in rcMonitor contains system windows such as the task bar and side bars.
            ///     Note that if the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative
            ///     values.
            /// </summary>
            public RectStruct WorkArea;

            /// <summary>
            ///     The attributes of the display monitor.
            ///     This member can be the following value:
            ///     1 : MONITORINFOF_PRIMARY
            /// </summary>
            public uint Flags;

            /// <summary>
            ///     A string that specifies the device name of the monitor being used. Most applications have no use for a display
            ///     monitor name,
            ///     and so can save some bytes by using a MONITORINFO structure.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string DeviceName;

            public void Init() {
                this.Size = 40 + 2 * CCHDEVICENAME;
                this.DeviceName = string.Empty;
            }
        }

        /// <summary>
        ///     The RECT structure defines the coordinates of the upper-left and lower-right corners of a rectangle.
        /// </summary>
        /// <see cref="http://msdn.microsoft.com/en-us/library/dd162897%28VS.85%29.aspx" />
        /// <remarks>
        ///     By convention, the right and bottom edges of the rectangle are normally considered exclusive.
        ///     In other words, the pixel whose coordinates are ( right, bottom ) lies immediately outside of the the rectangle.
        ///     For example, when RECT is passed to the FillRect function, the rectangle is filled up to, but not including,
        ///     the right column and bottom row of pixels. This structure is identical to the RECTL structure.
        /// </remarks>
        [StructLayout(LayoutKind.Sequential)]
        public struct RectStruct {
            /// <summary>
            ///     The x-coordinate of the upper-left corner of the rectangle.
            /// </summary>
            public int Left;

            /// <summary>
            ///     The y-coordinate of the upper-left corner of the rectangle.
            /// </summary>
            public int Top;

            /// <summary>
            ///     The x-coordinate of the lower-right corner of the rectangle.
            /// </summary>
            public int Right;

            /// <summary>
            ///     The y-coordinate of the lower-right corner of the rectangle.
            /// </summary>
            public int Bottom;
        }
    }
}