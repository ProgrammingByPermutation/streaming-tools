namespace streaming_tools.Views {
    using Avalonia.Controls;
    using Avalonia.Markup.Xaml;

    /// <summary>
    ///     The view responsible for managing the phonetic spellings of words.
    /// </summary>
    public class TtsPhoneticWordsView : UserControl {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TtsPhoneticWordsView" /> class.
        /// </summary>
        public TtsPhoneticWordsView() {
            this.InitializeComponent();
        }

        /// <summary>
        ///     Initializes the GUI components.
        /// </summary>
        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }
    }
}