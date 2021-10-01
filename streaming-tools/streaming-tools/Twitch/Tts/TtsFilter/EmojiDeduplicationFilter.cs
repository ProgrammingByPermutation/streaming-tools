namespace streaming_tools.Twitch.Tts.TtsFilter {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using TwitchLib.Client.Events;
    using Utilities;

    /// <summary>
    ///     Remove duplicate spammed emojis from TTS messages. Performs no admin actions on the user (such as banning or timing
    ///     out), simply removes the duplicate or excessive use of emotes from the message.
    /// </summary>
    public class EmojiDeduplicationFilter : ITtsFilter {
        /// <summary>
        ///     The maximum number of emotes to allow.
        /// </summary>
        private const int MAXIMUM_EMOTES = 2;


        /// <summary>
        ///     Removes duplicate emotes from a message.
        /// </summary>
        /// <param name="twitchInfo">The information on the original chat message.</param>
        /// <param name="username">The username of the twitch chatter for TTS to say.</param>
        /// <param name="currentMessage">The message from twitch chat.</param>
        /// <returns>The new TTS message and username.</returns>
        public Tuple<string, string> Filter(OnMessageReceivedArgs twitchInfo, string username, string currentMessage) {
            // The list of all recognized emotes.
            var emoteText = new HashSet<string>();

            // Get emotes from official twitch channel
            twitchInfo.ChatMessage.EmoteSet.Emotes.Select(e => emoteText.Add(e.Name.ToLowerInvariant())).ToArray();

            // BetterTTV emotes
            EmoteLookup.GetBetterTtvEmotes(twitchInfo.ChatMessage.RoomId).Select(e => emoteText.Add(e.ToLowerInvariant())).ToArray();

            // FrankerzFace emotes
            EmoteLookup.GetFrankerzFaceEmotes(twitchInfo.ChatMessage.Channel).Select(e => emoteText.Add(e.ToLowerInvariant())).ToArray();

            // Read emote only once
            var encounteredEmotes = new HashSet<string>();
            var messageParts = currentMessage.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < messageParts.Length; i++) {
                var part = messageParts[i].ToLowerInvariant().Trim();

                // Check for twitch, frankerzface, and betterttv
                var hasEmote = emoteText.Contains(part);

                // Check :<text>: emoji
                if (part.StartsWith(":") && part.EndsWith(":")) {
                    hasEmote = true;
                }

                // If the message is an emote.
                if (hasEmote) {
                    // If we have already encountered the emote, remove it from the message.
                    if (encounteredEmotes.Contains(part)) {
                        messageParts[i] = "";
                    } else {
                        // If we haven't not, add it to the list of things we've encountered for later.
                        encounteredEmotes.Add(part);

                        // If they used more than 2 emotes, remove it from the message.
                        if (encounteredEmotes.Count > EmojiDeduplicationFilter.MAXIMUM_EMOTES) {
                            messageParts[i] = "";
                        }
                    }

                    continue;
                }

                // Handle ASCII character emotes
                var charArray = part.ToCharArray();
                for (var x = 0; x < charArray.Length; x++) {
                    // Look at the character, everything outside of ascii letters (over 127) is an emote. Not technically true but good enough.
                    var possibleEmote = charArray[x];
                    if (possibleEmote > 127) {
                        // If we have already encountered the emote, remove it from the message.
                        if (encounteredEmotes.Contains(possibleEmote.ToString())) {
                            charArray[x] = ' ';
                        } else {
                            // If we haven't not, add it to the list of things we've encountered for later.
                            encounteredEmotes.Add(possibleEmote.ToString());

                            // If they used more than 2 emotes, remove it from the message.
                            // There is a slight problem with this. Some of the higher value emotes take up two character slots in unicode.
                            // technically this will cause us to shot change people using emotes. But this is good enough for now.
                            if (encounteredEmotes.Count > EmojiDeduplicationFilter.MAXIMUM_EMOTES) {
                                charArray[x] = ' ';
                            }
                        }
                    }
                }

                // Put the message back together again from the characters we split apart.
                messageParts[i] = string.Join("", charArray.Where(c => !char.IsWhiteSpace(c)));
            }

            return new Tuple<string, string>(username, string.Join(" ", messageParts.Where(m => !string.IsNullOrWhiteSpace(m))));
        }
    }
}