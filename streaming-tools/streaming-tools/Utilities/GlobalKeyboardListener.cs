namespace streaming_tools.Utilities {
    using System;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    ///     Listens for keystrokes across the entire OS.
    /// </summary>
    /// <remarks>
    ///     For the string representation of the keys see the mapping in:
    ///     <see href="https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes?redirectedfrom=MSDN" />
    /// </remarks>
    public class GlobalKeyboardListener {
        /// <summary>
        ///     The singleton instance of our class.
        /// </summary>
        private static GlobalKeyboardListener? instance;

        /// <summary>
        ///     Lock for the <see cref="keyboardListenerProcess" /> to prevent concurrent access.
        /// </summary>
        private readonly object keyboardListenerProcessLock = new();

        /// <summary>
        ///     Thread monitoring standard output of <seealso cref="keyboardListenerProcess" /> in order to give the keystrokes.
        /// </summary>
        private readonly Thread? thread;

        /// <summary>
        ///     The process we launched.
        /// </summary>
        private Process? keyboardListenerProcess;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GlobalKeyboardListener" /> class.
        /// </summary>
        protected GlobalKeyboardListener() {
            this.CreateListenerProcess();

            this.thread = new Thread(this.ReadKeyboardThread) { IsBackground = true };
            this.thread.Start();
        }

        /// <summary>
        ///     Gets the singleton instance of the class.
        /// </summary>
        public static GlobalKeyboardListener Instance {
            get {
                if (null == GlobalKeyboardListener.instance) {
                    GlobalKeyboardListener.instance = new GlobalKeyboardListener();
                }

                return GlobalKeyboardListener.instance;
            }
        }

        /// <summary>
        ///     Gets or sets the callbacks to invoke when a keystroke is pressed.
        /// </summary>
        public Action<string>? Callback { get; set; }

        /// <summary>
        ///     Creates the keyboard listening process.
        /// </summary>
        private void CreateListenerProcess() {
            lock (this.keyboardListenerProcessLock) {
                var startInfo = new ProcessStartInfo {
                    FileName = Constants.WINDOWS_KEYBOARD_HOOK_LOCATION,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    Arguments = Process.GetCurrentProcess().Id.ToString()
                };

                this.keyboardListenerProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
                this.keyboardListenerProcess.Exited += this.ListenerKeyboardListenerProcessOnExited;
                this.keyboardListenerProcess.Start();
            }
        }

        /// <summary>
        ///     Raised when the keyboard listening process exits to restart the process.
        /// </summary>
        /// <remarks>It is assumed that this process runs the entire time we run. If it exits, we need to restart it.</remarks>
        /// <param name="sender">The process that exited.</param>
        /// <param name="e">The event arguments.</param>
        private void ListenerKeyboardListenerProcessOnExited(object? sender, EventArgs e) {
            lock (this.keyboardListenerProcessLock) {
                if (null != this.keyboardListenerProcess) {
                    this.keyboardListenerProcess.Exited -= this.ListenerKeyboardListenerProcessOnExited;
                    this.keyboardListenerProcess.Dispose();
                    this.keyboardListenerProcess = null;
                }

                this.CreateListenerProcess();
            }
        }

        /// <summary>
        ///     The main entry point of the <see cref="thread" /> which listens for output from the
        ///     process.
        /// </summary>
        private void ReadKeyboardThread() {
            while (true) {
                var shouldSleep = false;

                lock (this.keyboardListenerProcessLock) {
                    if (null == this.keyboardListenerProcess) {
                        shouldSleep = true;
                    }

                    try {
                        string? line;
                        while (!shouldSleep && null != (line = this.keyboardListenerProcess?.StandardOutput?.ReadLine())) {
                            this.Callback?.Invoke(line);
                        }
                    } catch (Exception) {
                        shouldSleep = true;
                    }
                }

                if (shouldSleep) {
                    Thread.Sleep(1000);
                }
            }
        }
    }
}