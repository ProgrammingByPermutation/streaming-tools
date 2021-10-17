using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace streaming_tools.Views {
    public class KeystrokesCommandView : UserControl {
        public KeystrokesCommandView() {
            InitializeComponent();
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }
    }
}