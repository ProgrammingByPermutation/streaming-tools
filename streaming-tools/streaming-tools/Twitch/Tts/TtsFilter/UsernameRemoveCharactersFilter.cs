namespace streaming_tools.Twitch.Tts.TtsFilter {
    using System;
    using System.Text.RegularExpressions;
    using TwitchLib.Client.Events;

    /// <summary>
    ///     Removes _s and numbers from people's usernames for TTS.
    /// </summary>
    public class UsernameRemoveCharactersFilter : ITtsFilter {
        /// <summary>
        ///     Handles identifying usernames in chat messages (anything that starts with a @).
        /// </summary>
        private readonly Regex regexMessageUsernames = new Regex(@"[@][a-zA-Z]+[\S]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        ///     Handles removing numbers and underscores from usernames.
        /// </summary>
        private readonly Regex regexUsername = new Regex(@"[0-9_]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        ///     Updates usernames to remove the _s and numbers.
        /// </summary>
        /// <param name="twitchInfo">The information on the original chat message.</param>
        /// <param name="username">The username of the twitch chatter for TTS to say.</param>
        /// <param name="currentMessage">The message from twitch chat.</param>
        /// <returns>The new TTS message and username.</returns>
        public Tuple<string, string> Filter(OnMessageReceivedArgs twitchInfo, string username, string currentMessage) {
            foreach (Capture? usernameMatch in this.regexMessageUsernames.Matches(currentMessage)) {
                if (null == usernameMatch) {
                    continue;
                }

                currentMessage = currentMessage.Replace(usernameMatch.Value, this.regexUsername.Replace(usernameMatch.Value, " "));
            }

            return new Tuple<string, string>(this.regexUsername.Replace(username, " "), currentMessage);
        }
    }
}