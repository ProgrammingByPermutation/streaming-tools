namespace streaming_tools.Twitch.Tts.TtsFilter {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using TwitchLib.Client.Events;

    /// <summary>
    ///     Converts things to their phonetic spellings.
    /// </summary>
    internal class PhoneticFilter : ITtsFilter {
        /// <summary>
        ///     Converts things to their phonetic spelling for TTS.
        /// </summary>
        /// <param name="twitchInfo">The information on the original chat message.</param>
        /// <param name="username">The username of the twitch chatter for TTS to say.</param>
        /// <param name="currentMessage">The message from twitch chat.</param>
        /// <returns>The new TTS message and username.</returns>
        public Tuple<string, string> Filter(OnMessageReceivedArgs twitchInfo, string username, string currentMessage) {
            string message = currentMessage;

            if (null != Configuration.Instance.TtsPhoneticUsernames) {
                foreach (var usernameToPhonetic in Configuration.Instance.TtsPhoneticUsernames) {
                    message = message.Replace(usernameToPhonetic.Key, usernameToPhonetic.Value, StringComparison.InvariantCultureIgnoreCase);
                }

                var foundUsername = Configuration.Instance.TtsPhoneticUsernames.FirstOrDefault(k => twitchInfo.ChatMessage.DisplayName.Equals(k.Key, StringComparison.InvariantCultureIgnoreCase));
                if (!default(KeyValuePair<string, string>).Equals(foundUsername)) {
                    username = foundUsername.Value;
                }
            }

            return new Tuple<string, string>(username, message);
        }
    }
}