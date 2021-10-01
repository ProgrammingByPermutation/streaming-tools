namespace streaming_tools.Twitch.Admin {
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using TwitchLib.Client;
    using TwitchLib.Client.Events;

    /// <summary>
    ///     Handles banning the "Wanna become famous" bot.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
    internal class BotWannaBecomeFamous : IAdminFilter {
        /// <summary>
        ///     Handles banning the "Wanna become famous" bot message.
        /// </summary>
        /// <param name="config">The configuration for the twitch chat.</param>
        /// <param name="client">The twitch client.</param>
        /// <param name="messageInfo">The information on the chat message.</param>
        /// <returns>True if the message should be passed on, false if it should be discarded.</returns>
        public bool Handle(TwitchChatConfiguration config, TwitchClient client, OnMessageReceivedArgs messageInfo) {
            if (string.IsNullOrWhiteSpace(config.AccountUsername) || string.IsNullOrWhiteSpace(config.TwitchChannel)) {
                return true;
            }

            string chatMessage = messageInfo.ChatMessage.Message;
            if (chatMessage.Contains("Wanna become famous?", StringComparison.InvariantCultureIgnoreCase) &&
                (
                    Regex.IsMatch(chatMessage, Constants.REGEX_URL) ||
                    chatMessage.Contains("Buy", StringComparison.InvariantCultureIgnoreCase) &&
                    chatMessage.Contains("followers", StringComparison.InvariantCultureIgnoreCase) &&
                    chatMessage.Contains("primes", StringComparison.InvariantCultureIgnoreCase) &&
                    chatMessage.Contains("viewers", StringComparison.InvariantCultureIgnoreCase)
                )) {
                return false;
            }

            return true;
        }
    }
}