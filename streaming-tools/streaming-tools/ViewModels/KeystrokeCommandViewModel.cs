namespace streaming_tools.ViewModels {
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using ReactiveUI;
    using Twitch;
    using Utilities;

    /// <summary>
    ///     The view responsible for managing the command to execute on keystroke.
    /// </summary>
    public class KeystrokeCommandViewModel : ViewModelBase {
        /// <summary>
        ///     The command to write in chat.
        /// </summary>
        private string? command;

        /// <summary>
        ///     The key code of the keystroke.
        /// </summary>
        private int? keyCode;

        /// <summary>
        ///     The twitch chat to type in.
        /// </summary>
        private string? selectedTwitchChat;

        /// <summary>
        ///     True if we are currently setting the keystroke.
        /// </summary>
        private bool settingKeyCode;

        /// <summary>
        ///     The list of twitch chats we can join.
        /// </summary>
        private ObservableCollection<string> twitchChats;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KeystrokeCommandViewModel" /> class.
        /// </summary>
        public KeystrokeCommandViewModel() {
            var configs = Configuration.Instance.TwitchChatConfigs?.Where(c => null != c.TwitchChannel).Select(c => null != c.TwitchChannel ? c.TwitchChannel.ToString() : "");
            if (null != configs && configs.Any()) {
                this.twitchChats = new ObservableCollection<string>(configs);
            } else {
                this.twitchChats = new ObservableCollection<string>();
            }

            this.KeyCode = Configuration.Instance.KeystokeCommand?.KeyCode;
            this.Command = Configuration.Instance.KeystokeCommand?.Command;
            this.SelectedTwitchChat = Configuration.Instance.KeystokeCommand?.TwitchChat;

            GlobalKeyboardListener.Instance.Callback += this.OnKeystrokeReceived;
            this.PropertyChanged += this.SaveToConfiguration;
        }

        /// <summary>
        ///     Gets or sets the command to write in chat.
        /// </summary>
        public string? Command {
            get => this.command;
            set => this.RaiseAndSetIfChanged(ref this.command, value);
        }

        /// <summary>
        ///     Gets or sets the key code of the keystroke.
        /// </summary>
        public int? KeyCode {
            get => this.keyCode;
            set => this.RaiseAndSetIfChanged(ref this.keyCode, value);
        }

        /// <summary>
        ///     Gets or sets the twitch chat to type in.
        /// </summary>
        public string? SelectedTwitchChat {
            get => this.selectedTwitchChat;
            set => this.RaiseAndSetIfChanged(ref this.selectedTwitchChat, value);
        }

        /// <summary>
        ///     Gets or sets the list of twitch chats we can join.
        /// </summary>
        public ObservableCollection<string> TwitchChats {
            get => this.twitchChats;
            set => this.RaiseAndSetIfChanged(ref this.twitchChats, value);
        }

        /// <summary>
        ///     Gets the next keystroke from the keyboard.
        /// </summary>
        public void GetKeystroke() {
            this.settingKeyCode = true;
        }

        /// <summary>
        ///     Raised when keystrokes are received from the keyboard.
        /// </summary>
        /// <param name="keycode">The key code of the keystroke.</param>
        private void OnKeystrokeReceived(string keycode) {
            if (this.settingKeyCode) {
                this.settingKeyCode = false;
                int inKeyCode;
                if (int.TryParse(keycode, out inKeyCode)) {
                    this.KeyCode = inKeyCode;
                }

                return;
            }

            int receivedKeycode;
            if (string.IsNullOrWhiteSpace(this.SelectedTwitchChat) || null == this.KeyCode || string.IsNullOrWhiteSpace(this.Command) || !int.TryParse(keycode, out receivedKeycode) || this.KeyCode != receivedKeycode) {
                return;
            }

            var client = TwitchChatManager.Instance.GetTwitchChannelClient(this.SelectedTwitchChat);
            if (null == client) {
                return;
            }

            if (!client.IsConnected) {
                client.Reconnect();
            }

            client.SendMessage(this.SelectedTwitchChat, this.Command);
        }

        /// <summary>
        ///     Saves the currently entered information to the configuration.
        /// </summary>
        /// <param name="sender">This class' object.</param>
        /// <param name="e">The event arguments.</param>
        private void SaveToConfiguration(object? sender, PropertyChangedEventArgs e) {
            if (null == Configuration.Instance.KeystokeCommand) {
                return;
            }

            Configuration.Instance.KeystokeCommand.KeyCode = this.KeyCode;
            Configuration.Instance.KeystokeCommand.Command = this.Command;
            Configuration.Instance.KeystokeCommand.TwitchChat = this.SelectedTwitchChat;
            Configuration.Instance.WriteConfiguration();
        }
    }
}