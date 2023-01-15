using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace StreamingTools.Views; 

public partial class VersionWindow : Window {
    public VersionWindow() {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }
}