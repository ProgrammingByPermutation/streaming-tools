namespace streaming_tools.Twitch.Tts.TtsFilter {
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using TwitchLib.Client.Events;

    /// <summary>
    ///     Filters out commands from being spoken in chat.
    /// </summary>
    public class CommandFilter : ITtsFilter {
        /// <summary>
        ///     Matches on ! commands in chat.
        /// </summary>
        private readonly Regex commandRegex = new Regex(@"[!]{1}[\S]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        ///     The commands to always speak anyway.
        /// </summary>
        private readonly string[] commandWhitelist = { "!lurk", "!tts", "!unlurk" };

        /// <summary>
        ///     Handles filtering commands from being spoken in chat.
        /// </summary>
        /// <param name="twitchInfo">The information on the original chat message.</param>
        /// <param name="username">The username of the twitch chatter for TTS to say.</param>
        /// <param name="currentMessage">The message from twitch chat.</param>
        /// <returns>The new TTS message and username.</returns>
        public Tuple<string, string> Filter(OnMessageReceivedArgs twitchInfo, string username, string currentMessage) {
            if (currentMessage.StartsWith("!")) {
                var command = this.commandRegex.Match(currentMessage);
                if (!this.commandWhitelist.Contains(command.Value.ToLowerInvariant())) {
                    currentMessage = "";
                }
            }

            return new Tuple<string, string>(username, currentMessage);
        }
    }
}