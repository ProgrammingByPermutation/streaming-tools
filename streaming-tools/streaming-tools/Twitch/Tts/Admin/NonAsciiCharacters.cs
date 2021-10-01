namespace streaming_tools.Twitch.Admin {
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using TwitchLib.Client;
    using TwitchLib.Client.Events;
    using TwitchLib.Client.Extensions;
    using Utilities;

    /// <summary>
    ///     Handles banning users that user non-ascii characters.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
    internal class NonAsciiCharacters : IAdminFilter {
        /// <summary>
        ///     Handles banning users that user non-ascii characters.
        /// </summary>
        /// <param name="config">The configuration for the twitch chat.</param>
        /// <param name="client">The twitch client.</param>
        /// <param name="messageInfo">The information on the chat message.</param>
        /// <returns>True if the message should be passed on, false if it should be discarded.</returns>
        public bool Handle(TwitchChatConfiguration config, TwitchClient client, OnMessageReceivedArgs messageInfo) {
            string chatMessage = messageInfo.ChatMessage.Message;

            // First convert each character into their hex-sequence representation in Unicode. This will catch both
            // characters outside of the ASCII character set (> 127) and non-emojis. We only allow ASCII characters
            // and emojis.
            var convertedMessageCharacters = UnicodeUtilities.ConvertToUnicodeNumber(chatMessage);
            foreach (var character in convertedMessageCharacters) {
                // If there is no space, then just evaluate the single character.
                if (!character.Contains(" ")) {
                    var num = int.Parse(character, NumberStyles.HexNumber);
                    if (num > 127 && !UnicodeUtilities.IsEmoji(character)) {
                        return false;
                    }

                    continue;
                }

                // If there is a space, it can only possibly be an emoji to be valid.
                if (!UnicodeUtilities.IsEmoji(character)) {
                    return false;
                }
            }

            return true;
        }
    }
}