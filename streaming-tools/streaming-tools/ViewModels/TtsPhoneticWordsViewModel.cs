namespace streaming_tools.ViewModels {
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using DynamicData;
    using ReactiveUI;

    /// <summary>
    ///     The view responsible for maintaining the list of phonetic words.
    /// </summary>
    public class TtsPhoneticWordsViewModel : ViewModelBase {
        /// <summary>
        ///     The reference to the word we're currently editing, null if we are not editing.
        /// </summary>
        private PhoneticWord? editingPhonetic;

        /// <summary>
        ///     The user entered phonetic pronunciation of the word.
        /// </summary>
        private string? userEnteredPhonetic;

        /// <summary>
        ///     The user entered word to pronounce phonetically.
        /// </summary>
        private string? userEnteredWord;

        /// <summary>
        ///     The collection of all phonetic words.
        /// </summary>
        private ObservableCollection<PhoneticWord> wordsToPhonetics = new();

        /// <summary>
        ///     Initializes a new instance of the <see cref="TtsPhoneticWordsViewModel" /> class.
        /// </summary>
        public TtsPhoneticWordsViewModel() {
            this.RefreshListInConfig();

            if (null != Configuration.Instance.TtsPhoneticUsernames) {
                foreach (var pair in Configuration.Instance.TtsPhoneticUsernames) {
                    this.wordsToPhonetics.Add(new PhoneticWord(this, pair.Key, pair.Value));
                }
            }
        }

        /// <summary>
        ///     Gets or sets the user entered phonetic pronunciation of the word.
        /// </summary>
        public string? UserEnteredPhonetic {
            get => this.userEnteredPhonetic;
            set => this.RaiseAndSetIfChanged(ref this.userEnteredPhonetic, value);
        }

        /// <summary>
        ///     Gets or sets the user entered word to pronounce phonetically.
        /// </summary>
        public string? UserEnteredWord {
            get => this.userEnteredWord;
            set => this.RaiseAndSetIfChanged(ref this.userEnteredWord, value);
        }

        /// <summary>
        ///     Gets or sets the list of words/usernames and their phonetic pronunciations.
        /// </summary>
        public ObservableCollection<PhoneticWord> WordsToPhonetics {
            get => this.wordsToPhonetics;
            set => this.RaiseAndSetIfChanged(ref this.wordsToPhonetics, value);
        }

        /// <summary>
        ///     Cancels the editing of the current word.
        /// </summary>
        public void CancelEntry() {
            this.editingPhonetic = null;
            this.UserEnteredWord = "";
            this.UserEnteredPhonetic = "";
        }

        /// <summary>
        ///     Deletes the word from the list.
        /// </summary>
        /// <param name="word">The word to delete.</param>
        public void DeletePhonetic(string word) {
            if (string.IsNullOrWhiteSpace(word)) {
                return;
            }

            var entry = this.wordsToPhonetics.FirstOrDefault(w => word.Equals(w.Word));
            if (null == entry) {
                return;
            }

            this.wordsToPhonetics.Remove(entry);
            this.RemoveFromConfig(word);
        }

        /// <summary>
        ///     Edits an existing word.
        /// </summary>
        /// <param name="word">The word to edit.</param>
        public void EditPhonetic(string word) {
            if (string.IsNullOrWhiteSpace(word)) {
                return;
            }

            var entry = this.wordsToPhonetics.FirstOrDefault(w => word.Equals(w.Word));
            if (null == entry) {
                return;
            }

            this.editingPhonetic = entry;
            this.UserEnteredWord = entry.Word;
            this.UserEnteredPhonetic = entry.Phonetic;
        }

        /// <summary>
        ///     Saves the current word.
        /// </summary>
        public void SaveEntry() {
            if (string.IsNullOrWhiteSpace(this.UserEnteredWord) || string.IsNullOrWhiteSpace(this.UserEnteredPhonetic)) {
                return;
            }

            // If we are not currently editing.
            if (null == this.editingPhonetic) {
                // If the word already exists in the list and this would be a duplicate then change the existing word.
                var existing = this.wordsToPhonetics.FirstOrDefault(w => this.UserEnteredWord.Equals(w.Word, StringComparison.InvariantCultureIgnoreCase));
                if (null != existing) {
                    existing.Word = this.UserEnteredWord;
                    existing.Phonetic = this.UserEnteredPhonetic;
                } else {
                    // Otherwise, make a new word.
                    this.wordsToPhonetics.Add(new PhoneticWord(this, this.UserEnteredWord, this.UserEnteredPhonetic));
                }
            } else {
                // If we are currently editing, update the existing word.
                this.editingPhonetic.Word = this.UserEnteredWord;
                this.editingPhonetic.Phonetic = this.UserEnteredPhonetic;
            }

            // The collection that holds the words contains immutable objects so we need to remove it and add it back in.
            this.RemoveFromConfig(this.UserEnteredWord);
            Configuration.Instance.TtsPhoneticUsernames?.Add(new KeyValuePair<string, string>(this.UserEnteredWord, this.UserEnteredPhonetic));
            this.editingPhonetic = null;
            this.UserEnteredWord = "";
            this.UserEnteredPhonetic = "";
            Configuration.Instance.WriteConfiguration();
        }

        /// <summary>
        ///     Cleans up the phonetic word list from the configuration.
        /// </summary>
        private void RefreshListInConfig() {
            if (null == Configuration.Instance.TtsPhoneticUsernames) {
                return;
            }

            var dict = new Dictionary<string, string>();
            foreach (var pair in Configuration.Instance.TtsPhoneticUsernames) {
                dict[pair.Key] = pair.Value;
            }

            var list = dict.ToList();
            list.Sort((pair, valuePair) => pair.Key.CompareTo(valuePair.Key));
            Configuration.Instance.TtsPhoneticUsernames.Clear();
            Configuration.Instance.TtsPhoneticUsernames.AddRange(list);
            Configuration.Instance.WriteConfiguration();
        }

        /// <summary>
        ///     Removes a word from the configuration.
        /// </summary>
        /// <param name="word">The word to remove.</param>
        private void RemoveFromConfig(string word) {
            if (string.IsNullOrWhiteSpace(word)) {
                return;
            }

            var existing = Configuration.Instance.TtsPhoneticUsernames?.FirstOrDefault(u => word.Equals(u.Key, StringComparison.InvariantCultureIgnoreCase));
            if (null != existing && !default(KeyValuePair<string, string>).Equals(existing)) {
                Configuration.Instance.TtsPhoneticUsernames?.Remove(existing.Value);
                Configuration.Instance.WriteConfiguration();
            }
        }

        /// <summary>
        ///     A representation of a word that needs to be pronounced phonetically.
        /// </summary>
        public class PhoneticWord : ViewModelBase {
            /// <summary>
            ///     The view model that owns this object.
            /// </summary>
            private readonly TtsPhoneticWordsViewModel viewModel;

            /// <summary>
            ///     The phonetic pronunciation of the word.
            /// </summary>
            private string phonetic;

            /// <summary>
            ///     The word to pronounce phonetically.
            /// </summary>
            private string word;

            /// <summary>
            ///     Initializes a new instance of the <see cref="PhoneticWord" /> class.
            /// </summary>
            /// <param name="viewModel"> The view model that owns this object.</param>
            /// <param name="word">The word to pronounce phonetically.</param>
            /// <param name="phonetic">The phonetic pronunciation of the word.</param>
            public PhoneticWord(TtsPhoneticWordsViewModel viewModel, string word, string phonetic) {
                this.viewModel = viewModel;
                this.word = word;
                this.phonetic = phonetic;
            }

            /// <summary>
            ///     Gets or sets the phonetic pronunciation of the word.
            /// </summary>
            public string Phonetic {
                get => this.phonetic;
                set => this.RaiseAndSetIfChanged(ref this.phonetic, value);
            }

            /// <summary>
            ///     Gets or sets the word to pronounce phonetically.
            /// </summary>
            public string Word {
                get => this.word;
                set => this.RaiseAndSetIfChanged(ref this.word, value);
            }

            /// <summary>
            ///     Deletes this word from the list.
            /// </summary>
            public void DeletePhonetic() {
                this.viewModel.DeletePhonetic(this.word);
            }

            /// <summary>
            ///     Edits this word in the list.
            /// </summary>
            public void EditPhonetic() {
                this.viewModel.EditPhonetic(this.word);
            }
        }
    }
}