namespace streaming_tools.ViewModels {
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using ReactiveUI;
    using Twitch;
    using Twitch.Tts;
    using TwitchLib.Client.Extensions;
    using Utilities;

    /// <summary>
    ///     The view responsible for managing the command to execute on keystroke.
    /// </summary>
    public class KeystrokeCommandViewModel : ViewModelBase {
        private bool banUserCurrentMessage;
        private bool clearMessageQueue;
        

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

        private bool sendChatMessage;

        /// <summary>
        ///     True if we are currently setting the keystroke.
        /// </summary>
        private bool settingKeyCode;

        private bool skipMessage;

        private bool timeoutUserCurrentMessage;

        private Action<KeystrokeCommandViewModel> deleteCallback;

        /// <summary>
        ///     The list of twitch chats we can join.
        /// </summary>
        private ObservableCollection<string> twitchChats;

        private KeystokeCommand config;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KeystrokeCommandViewModel" /> class.
        /// </summary>
        public KeystrokeCommandViewModel(KeystokeCommand config, Action<KeystrokeCommandViewModel> deleteCallback) {
            this.Config = config;
            this.KeyCode = config.KeyCode;
            this.Command = config.Command;
            this.SelectedTwitchChat = config.TwitchChat;
            this.ClearMessageQueue = config.ClearMessageQueue;
            this.TimeoutUserCurrentMessage = config.TimeoutUserCurrentMessage;
            this.BanUserCurrentMessage = config.BanUserCurrentMessage;
            this.SendChatMessage = config.SendChatMessage;
            this.deleteCallback = deleteCallback;

            GlobalKeyboardListener.Instance.Callback += this.OnKeystrokeReceived;
            this.PropertyChanged += this.SaveToConfiguration;
        }

        /// <summary>
        /// Gets or sets the configuration for the item.
        /// </summary>
        public KeystokeCommand Config {
            get => this.config;
            set => this.RaiseAndSetIfChanged(ref this.config, value);
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
        ///     Gets or sets a value indicating whether this is a keystroke to skip the current message.
        /// </summary>
        public bool SkipMessage {
            get => this.skipMessage;
            set => this.RaiseAndSetIfChanged(ref this.skipMessage, value);
        }

        /// <summary>
        ///     Gets or sets a value indicating whether this is a keystroke to skip all current messages.
        /// </summary>
        public bool ClearMessageQueue {
            get => this.clearMessageQueue;
            set => this.RaiseAndSetIfChanged(ref this.clearMessageQueue, value);
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the user that typed the current message should be timed out.
        /// </summary>
        public bool TimeoutUserCurrentMessage {
            get => this.timeoutUserCurrentMessage;
            set => this.RaiseAndSetIfChanged(ref this.timeoutUserCurrentMessage, value);
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the user that typed the current message should be banned.
        /// </summary>
        public bool BanUserCurrentMessage {
            get => this.banUserCurrentMessage;
            set => this.RaiseAndSetIfChanged(ref this.banUserCurrentMessage, value);
        }

        /// <summary>
        ///     Gets or sets a value indicating whether this is a keystroke to send a message in chat.
        /// </summary>
        public bool SendChatMessage {
            get => this.sendChatMessage;
            set => this.RaiseAndSetIfChanged(ref this.sendChatMessage, value);
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

        public void Delete() {
            this.deleteCallback?.Invoke(this);
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
            if (string.IsNullOrWhiteSpace(this.SelectedTwitchChat) ||
                null == this.KeyCode ||
                string.IsNullOrWhiteSpace(this.Command) ||
                !int.TryParse(keycode, out receivedKeycode) || this.KeyCode != receivedKeycode) {
                return;
            }

            if (this.SendChatMessage) {
                this.HandleSendChatMessage();
            } else if (this.SkipMessage) {
                this.HandleSkipMessage();
            } else if (this.ClearMessageQueue) {
                this.HandleClearMessageQueue();
            } else if (this.TimeoutUserCurrentMessage) {
                this.HandleTimeoutUserCurrentMessage();
            } else if (this.BanUserCurrentMessage) {
                this.HandleBanUserCurrentMessage();
            }
        }

        private void HandleSkipMessage() {
            var account = Configuration.Instance.TwitchAccounts?.FirstOrDefault(a => a.IsUsersStreamingAccount);
            if (null == account || null == account.Username) {
                return;
            }

            TwitchChatTts? tts;
            if (TwitchChatConfigViewModel.TTS_CACHE.TryGetValue(account.Username.ToLowerInvariant(), out tts)) {
                tts.SkipCurrentTts();
            }
        }

        private void HandleClearMessageQueue() {
            var account = Configuration.Instance.TwitchAccounts?.FirstOrDefault(a => a.IsUsersStreamingAccount);
            if (null == account || null == account.Username) {
                return;
            }

            TwitchChatTts? tts;
            if (TwitchChatConfigViewModel.TTS_CACHE.TryGetValue(account.Username.ToLowerInvariant(), out tts)) {
                tts.SkipAllTts();
            }
        }

        private void HandleTimeoutUserCurrentMessage() {
            var account = Configuration.Instance.TwitchAccounts?.FirstOrDefault(a => a.IsUsersStreamingAccount);
            if (null == account || null == account.Username) {
                return;
            }

            TwitchChatTts? tts;
            if (!TwitchChatConfigViewModel.TTS_CACHE.TryGetValue(account.Username.ToLowerInvariant(), out tts)) {
                return;
            }

            var username = tts.CurrentUsername;
            if (string.IsNullOrWhiteSpace(username)) {
                return;
            }

            this.HandleSkipMessage();

            if (string.IsNullOrWhiteSpace(tts.ChatConfig?.TwitchChannel)) {
                return;
            }

            var client = TwitchChatManager.Instance.GetTwitchChannelClient(tts.ChatConfig.TwitchChannel);
            if (null == client) {
                return;
            }

            if (!client.IsConnected) {
                client.Reconnect();
            }

            client.TimeoutUser(tts.ChatConfig.TwitchChannel, username, TimeSpan.FromSeconds(1));
        }

        private void HandleBanUserCurrentMessage() {
            var account = Configuration.Instance.TwitchAccounts?.FirstOrDefault(a => a.IsUsersStreamingAccount);
            if (null == account || null == account.Username) {
                return;
            }

            TwitchChatTts? tts;
            if (!TwitchChatConfigViewModel.TTS_CACHE.TryGetValue(account.Username.ToLowerInvariant(), out tts)) {
                return;
            }

            var username = tts.CurrentUsername;
            if (string.IsNullOrWhiteSpace(username)) {
                return;
            }

            this.HandleSkipMessage();

            if (string.IsNullOrWhiteSpace(tts.ChatConfig?.TwitchChannel)) {
                return;
            }

            var client = TwitchChatManager.Instance.GetTwitchChannelClient(tts.ChatConfig.TwitchChannel);
            if (null == client) {
                return;
            }

            if (!client.IsConnected) {
                client.Reconnect();
            }

            client.BanUser(tts.ChatConfig.TwitchChannel, username);
        }

        private void HandleSendChatMessage() {
            if (null == this.SelectedTwitchChat) {
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
            if (null == Configuration.Instance.KeystrokeCommand) {
                return;
            }
            
            config.KeyCode = this.KeyCode;
            config.Command = this.Command;
            config.TwitchChat = this.SelectedTwitchChat;
            config.ClearMessageQueue = this.ClearMessageQueue;
            config.TimeoutUserCurrentMessage = this.TimeoutUserCurrentMessage;
            config.BanUserCurrentMessage = this.BanUserCurrentMessage;
            config.SendChatMessage = this.SendChatMessage;
            Configuration.Instance.WriteConfiguration();
        }
    }
}