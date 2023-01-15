using System;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ReactiveUI;
using StreamingTools.Updates;
using StreamingTools.ViewModels;

namespace StreamingTools.Views;

public partial class MainWindow : Window {
    private MainWindowViewModel? _viewModel;
    
    public MainWindow() {
        _viewModel = new MainWindowViewModel {
            Parent = this
        };

        this.DataContext = _viewModel;
        InitializeComponent();
        RxApp.MainThreadScheduler.Schedule(async () => await this._viewModel.CheckForNewVersion());
    }

    
}