namespace streaming_tools.Twitch.Admin {
    using TwitchLib.Client;
    using TwitchLib.Client.Events;

    /// <summary>
    ///     Handles the admin filters on a twitch chat.
    /// </summary>
    public class TwitchChatAdmin {
        /// <summary>
        ///     The ordered filters to apply to administer the twitch chat.
        /// </summary>
        private readonly IAdminFilter[] adminFilters = { new BotWannaBecomeFamous(), new NonAsciiCharacters() };

        /// <summary>
        ///     The chat configuration.
        /// </summary>
        private readonly TwitchChatConfiguration? chatConfig;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TwitchChatAdmin" /> class.
        /// </summary>
        /// <param name="config">The chat configuration.</param>
        public TwitchChatAdmin(TwitchChatConfiguration? config) {
            this.chatConfig = config;
        }

        /// <summary>
        ///     Connects to the chat to listen for messages to apply admin filters to.
        /// </summary>
        public void Connect() {
            if (null == this.chatConfig) {
                return;
            }

            var config = Configuration.Instance;
            var user = config.GetTwitchAccount(this.chatConfig.AccountUsername);
            if (null == user) {
                return;
            }

            var twitchManager = TwitchChatManager.Instance;
            twitchManager.AddTwitchChannelAdminFilter(user, this.chatConfig.TwitchChannel, this.Client_OnMessageReceived);
        }

        /// <summary>
        ///     Releases unmanaged resources.
        /// </summary>
        public void Dispose() {
            if (null == this.chatConfig) {
                return;
            }

            var user = Configuration.Instance.GetTwitchAccount(this.chatConfig.AccountUsername);
            if (null == user) {
                return;
            }

            var twitchManager = TwitchChatManager.Instance;
            twitchManager.RemoveTwitchChannelAdminFilter(user, this.chatConfig.TwitchChannel, this.Client_OnMessageReceived);
        }

        /// <summary>
        ///     Applies admin filters to the messages from twitch chat.
        /// </summary>
        /// <param name="twitchClient">The twitch chat client.</param>
        /// <param name="e">The message.</param>
        /// <returns>True if the message should be passed on, false otherwise.</returns>
        private bool Client_OnMessageReceived(TwitchClient twitchClient, OnMessageReceivedArgs e) {
            if (null == this.chatConfig) {
                return true;
            }

            // First apply any administration filters where we may need to ban people from 
            // chat, etc. If the administration filter tells us that we shouldn't process
            // the message further because it handled it, then don't.
            foreach (var filter in this.adminFilters) {
                if (!filter.Handle(this.chatConfig, twitchClient, e)) {
                    return false;
                }
            }

            return true;
        }
    }
}