namespace StreamingTools.ViewModels; 

public class VersionViewModel : ViewModelBase {
    public string NewVersionUrl { get; set; }
    
    public VersionViewModel(string newVersionUrl) {
        this.NewVersionUrl = newVersionUrl;
    }
    
    public void ShowNewVersionWindow() {
        System.Diagnostics.Process.Start("cmd", $"/C start {this.NewVersionUrl}");
    }
}