namespace WindowsKeyboardHook {
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;
    using PInvoke;

    /// <summary>
    ///     A console application that hooks into the global keyboard keystrokes on the OS and outputs them to STDOUT.
    /// </summary>
    /// <remarks>
    ///     See:
    ///     <see href="https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes?redirectedfrom=MSDN" />
    /// </remarks>
    internal class Program {
        /// <summary>
        ///     Information on what kind of event it is.
        /// </summary>
        public enum KeyboardMessage {
            /// <summary>
            ///     The key was pressed down.
            /// </summary>
            KeyDown = 0x100,

            /// <summary>
            ///     The key was released after being pressed down.
            /// </summary>
            KeyUp = 0x101,

            /// <summary>
            ///     No idea.
            /// </summary>
            SysKeyDown = 0x104,

            /// <summary>
            ///     Don't ask me.
            /// </summary>
            SysKeyUp = 0x105
        }

        /// <summary>
        ///     The pointer to the hook we added.
        /// </summary>
        private static User32.SafeHookHandle hook;

        /// <summary>
        ///     The event invoked when a key is pressed.
        /// </summary>
        /// <param name="nCode">The code.</param>
        /// <param name="wParam">The <seealso cref="KeyboardMessage" />.</param>
        /// <param name="lParam">The <seealso cref="KeyboardLowLevelHookStruct" />.</param>
        /// <returns>The next hook that should be called.</returns>
        private static int KeystrokeCallback(int nCode, IntPtr wParam, IntPtr lParam) {
            var keyboardEvent = Marshal.PtrToStructure<KeyboardLowLevelHookStruct>(lParam);
            var whatHappened = (KeyboardMessage)wParam;

            if (whatHappened == KeyboardMessage.KeyDown) {
                Console.WriteLine(keyboardEvent.vkCode);
            }

            return User32.CallNextHookEx(Program.hook.DangerousGetHandle(), nCode, wParam, lParam);
        }

        /// <summary>
        ///     The main entry point to the application that listens to the keystrokes across the entire OS.
        /// </summary>
        /// <param name="args">We don't use arguments.</param>
        private static void Main(string[] args) {
            var pid = -1;
            if (args.Length > 0) {
                int.TryParse(args[0], out pid);
            }

            var thread = new Thread(
                () => {
                    while (true) {
                        if (-1 == pid) {
                            return;
                        }

                        try {
                            var process = Process.GetProcessById(pid);
                            if (process.HasExited) {
                                Environment.Exit(0);
                                return;
                            }
                        } catch (Exception) {
                            Environment.Exit(0);
                            return;
                        }

                        Thread.Sleep(500);
                    }
                });
            thread.IsBackground = true;
            thread.Start();

            // Set a hook.
            Program.hook = User32.SetWindowsHookEx(User32.WindowsHookType.WH_KEYBOARD_LL, Program.KeystrokeCallback, IntPtr.Zero, 0);

            unsafe {
                // Buffer the messages.
                User32.MSG message;
                while (0 != User32.GetMessage(&message, IntPtr.Zero, 0, 0)) { }
            }
        }

        /// <summary>
        ///     The structure representing the key pressed.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct KeyboardLowLevelHookStruct {
            /// <summary>
            ///     The keystroke.
            /// </summary>
            public int vkCode;

            /// <summary>
            ///     The scan code.
            /// </summary>
            public int scanCode;

            /// <summary>
            ///     The flags.
            /// </summary>
            public int flags;

            /// <summary>
            ///     The time the key was pressed.
            /// </summary>
            public int time;

            /// <summary>
            ///     The extra info.
            /// </summary>
            public IntPtr dwExtraInfo;
        }
    }
}