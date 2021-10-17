namespace streaming_tools.ViewModels {
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using ReactiveUI;
    using Twitch;
    using Twitch.Tts;
    using TwitchLib.Client.Extensions;
    using Utilities;
    using Views;

    /// <summary>
    ///     The view responsible for managing the command to execute on keystroke.
    /// </summary>
    public class KeystrokesCommandViewModel : ViewModelBase {
        private ObservableCollection<KeystrokeCommandView> views;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KeystrokeCommandViewModel" /> class.
        /// </summary>
        public KeystrokesCommandViewModel() {
            views = new ObservableCollection<KeystrokeCommandView>();
            if (null == Configuration.Instance.KeystrokeCommand)
                return;
            
            foreach (var config in Configuration.Instance.KeystrokeCommand) {
                views.Add(
                    new KeystrokeCommandView {
                        DataContext = new KeystrokeCommandViewModel(config, DeleteKeystrokeCommand)
                    }
                );
            }
        }

        /// <summary>
        ///     Gets or sets the command to write in chat.
        /// </summary>
        public ObservableCollection<KeystrokeCommandView> Views {
            get => this.views;
            set => this.RaiseAndSetIfChanged(ref this.views, value);
        }

        public void AddKeystrokeCommand() {
            var config = new KeystokeCommand();
            Configuration.Instance.KeystrokeCommand?.Add(config);
            Configuration.Instance.WriteConfiguration();
            views.Add(
                new KeystrokeCommandView {
                    DataContext = new KeystrokeCommandViewModel(config, DeleteKeystrokeCommand)
                }
            );
        }

        public void DeleteKeystrokeCommand(KeystrokeCommandViewModel viewModel) {
            Configuration.Instance.KeystrokeCommand?.Remove(viewModel.Config);
            Configuration.Instance.WriteConfiguration();

            var view = this.views.FirstOrDefault(v => v.DataContext?.Equals(viewModel) ?? false);
            if (null == view) {
                return;
            }
            
            views.Remove(view);
        }
    }
}