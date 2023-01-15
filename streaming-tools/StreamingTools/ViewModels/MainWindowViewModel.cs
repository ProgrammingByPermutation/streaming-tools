using System;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using StreamingTools.Updates;
using StreamingTools.Views;

namespace StreamingTools.ViewModels;

public class MainWindowViewModel : ViewModelBase {
    private Window parent;

    public Window Parent {
        get { return parent; }
        set { parent = value; }
    }

    public async Task CheckForNewVersion() {
        var version = await UpdateManager.GetLatestVersion();
        if (!new Version(version.name).Equals(Assembly.GetEntryAssembly().GetName().Version)) {
            var versionDialog = new VersionWindow() {
                DataContext = new VersionViewModel(version.html_url)
            };
            
            versionDialog.ShowDialog(this.Parent);
        }
    }
}