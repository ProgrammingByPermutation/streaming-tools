namespace streaming_tools.Utilities {
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    ///     Miscellaneous functions for dealing with unicode.
    /// </summary>
    public static class UnicodeUtilities {
        /// <summary>
        ///     A cache of hexadecimal unicode sequences that equal emoji.
        /// </summary>
        private static HashSet<string>? EMOJI_HEX_CODES;

        /// <summary>
        ///     Converts each character in a string to a collection of the hexadecimal sequences representing each character.
        /// </summary>
        /// <param name="str">The string to decompose into hexadecimal sequences.</param>
        /// <returns>A collection of hexadecimal unicode sequences representing each character in order from the string.</returns>
        public static IEnumerable<string> ConvertToUnicodeNumber(string str) {
            // This class is fucking stupid and doesn't implement IEnumerable even though
            // it is an enumerable because fuck me I guess. We have to stop using the real
            // enumerable code and manually enumerate it like we have a learning disability.
            // Yay.
            var characterSequences = new List<string>();
            var enumerator = StringInfo.GetTextElementEnumerator(str);
            while (enumerator.MoveNext()) {
                var currentCharacter = enumerator.Current.ToString();
                if (null == currentCharacter) {
                    continue;
                }

                var curr = "";
                for (var i = 0; i < currentCharacter.Length; i++) {
                    if (char.IsHighSurrogate(currentCharacter[i]) && i + 1 < currentCharacter.Length) {
                        var rune = new Rune(currentCharacter[i], currentCharacter[i + 1]);
                        curr += $"{rune.Value:X4} ";
                        ++i;
                    } else {
                        curr += $"{(int)currentCharacter[i]:X4} ";
                    }
                }

                characterSequences.Add(curr.Trim());
            }

            return characterSequences;
        }

        /// <summary>
        ///     Generates a new file representing the hexadecimal sequences of every emoji.
        /// </summary>
        /// <param name="output">The file to write to.</param>
        /// <returns>An async task.</returns>
        public static async Task GenerateEmojiFile(string output) {
            if (File.Exists(output)) {
                File.Delete(output);
            }

            var httpPages = new[] {
                "https://unicode.org/Public/emoji/14.0/emoji-sequences.txt",
                "https://unicode.org/Public/emoji/14.0/emoji-test.txt",
                "https://unicode.org/Public/emoji/14.0/emoji-zwj-sequences.txt"
            };

            var emojiHexCodes = new HashSet<string>();
            foreach (var page in httpPages) {
                using (HttpClient client = new HttpClient()) {
                    var content = await client.GetStreamAsync(page);
                    using (StreamReader reader = new StreamReader(content)) {
                        string? line;
                        while (null != (line = await reader.ReadLineAsync())) {
                            line = line.Trim();
                            if (line.StartsWith("#") || !line.Contains(";")) {
                                continue;
                            }

                            var index = line.IndexOf(";", StringComparison.InvariantCultureIgnoreCase);
                            var currentRule = line.Substring(0, index).Trim();
                            if (currentRule.Contains("..")) {
                                var parts = currentRule.Split("..");
                                var begin = int.Parse(parts[0], NumberStyles.HexNumber);
                                var end = int.Parse(parts[1], NumberStyles.HexNumber);

                                for (var i = begin; i <= end; i++) {
                                    emojiHexCodes.Add($"{i:X4}");
                                }

                                continue;
                            }

                            emojiHexCodes.Add(currentRule);
                        }
                    }
                }
            }

            var outputList = new List<string>(emojiHexCodes);
            outputList.Sort();
            using (var writer = new StreamWriter(output)) {
                foreach (var hexCode in outputList) {
                    writer.WriteLine(hexCode);
                }
            }
        }

        /// <summary>
        ///     Determines if the hex sequence passed in is an emoji.
        /// </summary>
        /// <param name="hexSequence">The hexadecimal sequence to check.</param>
        /// <returns>True if its an emoji, false otherwise.</returns>
        public static bool IsEmoji(string hexSequence) {
            if (null == UnicodeUtilities.EMOJI_HEX_CODES) {
                UnicodeUtilities.EMOJI_HEX_CODES = new HashSet<string>();
                using (var reader = new StreamReader("Assets/emojiHexCodes.txt")) {
                    string? line;
                    while (null != (line = reader.ReadLine())) {
                        UnicodeUtilities.EMOJI_HEX_CODES.Add(line.Trim());
                    }
                }

                using (var reader = new StreamReader("Assets/additionalHexCodeWhitelist.txt")) {
                    string? line;
                    while (null != (line = reader.ReadLine())) {
                        UnicodeUtilities.EMOJI_HEX_CODES.Add(line.Trim());
                    }
                }
            }

            return UnicodeUtilities.EMOJI_HEX_CODES.Contains(hexSequence.Trim());
        }
    }
}