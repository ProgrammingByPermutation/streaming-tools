namespace streaming_tools.Utilities {
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;

    /// <summary>
    ///     Helper methods for looking up the emotes available on a channel.
    /// </summary>
    public static class EmoteLookup {
        /// <summary>
        ///     The cache of Better TTV emotes for each channel.
        /// </summary>
        private static readonly Dictionary<string, string[]> betterTtvCache = new Dictionary<string, string[]>();

        /// <summary>
        ///     The cache of FrankerzFace emotes for each channel.
        /// </summary>
        private static readonly Dictionary<string, string[]> frankerzFaceCache = new Dictionary<string, string[]>();

        /// <summary>
        ///     Gets the FrankerzFace emotes for the channel.
        /// </summary>
        /// <param name="channel">The channel to look up the emotes for.</param>
        /// <returns>An enumerable of enabled emotes if found, an empty enumerable otherwise.</returns>
        public static IEnumerable<string> GetFrankerzFaceEmotes(string channel) {
            // Try to use the emotes in the cache first.
            if (EmoteLookup.frankerzFaceCache.ContainsKey(channel)) {
                return EmoteLookup.frankerzFaceCache[channel];
            }

            // Query the API for the list of shared emotes
            HttpClient client = new HttpClient();
            var httpRequest = client.GetAsync($"https://api.frankerfacez.com/v1/room/{channel}");
            Task.WaitAny(httpRequest);
            var pageContent = httpRequest.Result.Content.ReadAsStringAsync();
            Task.WaitAny(pageContent);
            var pageContentJson = JObject.Parse(pageContent.Result);

            EmoteLookup.frankerzFaceCache[channel] = pageContentJson["sets"]?.FirstOrDefault()?.FirstOrDefault()?["emoticons"]?
                .Where(e => null != e["name"]?.Value<string>())
                // ReSharper disable once RedundantEnumerableCastCall
                .Select(e => e["name"]?.Value<string>()).Cast<string>().ToArray() ?? Enumerable.Empty<string>().ToArray();
            return EmoteLookup.frankerzFaceCache[channel];
        }

        /// <summary>
        ///     Gets the Better TTV emotes for the channel.
        /// </summary>
        /// <param name="roomId">The numeric twitch ID of the channel.</param>
        /// <returns>An enumerable of enabled emotes if found, an empty enumerable otherwise.</returns>
        public static IEnumerable<string> GetBetterTtvEmotes(string roomId) {
            // Try to use the emotes in the cache first.
            if (EmoteLookup.betterTtvCache.ContainsKey(roomId)) {
                return EmoteLookup.betterTtvCache[roomId];
            }

            // Query the API for the list of personal and shared emotes
            HttpClient client = new HttpClient();
            var httpRequest = client.GetAsync($"https://api.betterttv.net/3/cached/users/twitch/{roomId}");
            Task.WaitAny(httpRequest);
            var pageContent = httpRequest.Result.Content.ReadAsStringAsync();
            Task.WaitAny(pageContent);
            var pageContentJson = JObject.Parse(pageContent.Result);
            var channelEmotes = pageContentJson["channelEmotes"]?.Select(e => e["code"]?.Value<string>()) ?? Enumerable.Empty<string>();
            var sharedEmotes = pageContentJson["sharedEmotes"]?.Select(e => e["code"]?.Value<string>()) ?? Enumerable.Empty<string>();

            // ReSharper disable once RedundantEnumerableCastCall
            EmoteLookup.betterTtvCache[roomId] = channelEmotes.Concat(sharedEmotes).Cast<string>().ToArray();
            return EmoteLookup.betterTtvCache[roomId];
        }
    }
}