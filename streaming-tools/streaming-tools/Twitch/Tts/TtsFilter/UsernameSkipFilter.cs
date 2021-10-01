namespace streaming_tools.Twitch.Tts.TtsFilter {
    using System;
    using TwitchLib.Client.Events;

    /// <summary>
    ///     Filters chat messages based on the users that sent them.
    /// </summary>
    internal class UsernameSkipFilter : ITtsFilter {
        /// <summary>
        ///     Filters out chat messages for bot users.
        /// </summary>
        /// <param name="twitchInfo">The information on the original chat message.</param>
        /// <param name="username">The username of the twitch chatter for TTS to say.</param>
        /// <param name="currentMessage">The message from twitch chat.</param>
        /// <returns>The new TTS message and username.</returns>
        public Tuple<string, string> Filter(OnMessageReceivedArgs twitchInfo, string username, string currentMessage) {
            if (null == Configuration.Instance.TtsUsernamesToSkip) {
                return new Tuple<string, string>(username, currentMessage);
            }

            foreach (var ignoredUser in Configuration.Instance.TtsUsernamesToSkip) {
                if (ignoredUser.Equals(twitchInfo.ChatMessage.DisplayName, StringComparison.InvariantCultureIgnoreCase)) {
                    return new Tuple<string, string>("", "");
                }
            }

            return new Tuple<string, string>(username, currentMessage);
        }
    }
}