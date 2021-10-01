namespace streaming_tools.ViewModels {
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Timers;
    using DynamicData;
    using ReactiveUI;
    using Twitch;

    /// <summary>
    ///     Handles managing the list of users to skip in TTS.
    /// </summary>
    public class TtsSkipUsernamesViewModel : ViewModelBase {
        /// <summary>
        ///     A timer that handles periodically refreshing the twitch chat viewer list.
        /// </summary>
        private readonly Timer userListRefreshTimer;

        /// <summary>
        ///     The view model that handles the two column control.
        /// </summary>
        private TwoListViewModel twoListViewModel;

        /// <summary>
        ///     The user entered username to add to the skip list.
        /// </summary>
        private string? userToAdd;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TtsSkipUsernamesViewModel" /> class.
        /// </summary>
        public TtsSkipUsernamesViewModel() {
            this.userListRefreshTimer = new Timer(1);
            this.userListRefreshTimer.AutoReset = false;
            this.userListRefreshTimer.Elapsed += this.UserListRefreshTimer_OnElapsed;

            this.twoListViewModel = new TwoListViewModel { RightListBehavior = TwoListViewModel.DoubleClickBehavior.DeleteFromList, SortLeftList = true, SortRightList = true };

            if (null != Configuration.Instance.TtsUsernamesToSkip) {
                foreach (var username in Configuration.Instance.TtsUsernamesToSkip) {
                    this.TwoListViewModel.AddRightList(username);
                }
            }

            this.TwoListViewModel.RightList.CollectionChanged += this.TtsSkipped_OnCollectionChanged;
            this.userListRefreshTimer.Start();
        }

        /// <summary>
        ///     Gets or sets the view model that handles the two column control.
        /// </summary>
        public TwoListViewModel TwoListViewModel {
            get => this.twoListViewModel;
            set => this.RaiseAndSetIfChanged(ref this.twoListViewModel, value);
        }

        /// <summary>
        ///     Gets or sets the user entered username to add to the skip list.
        /// </summary>
        public string? UserToAdd {
            get => this.userToAdd;
            set => this.RaiseAndSetIfChanged(ref this.userToAdd, value);
        }

        /// <summary>
        ///     Handles adding the <see cref="UserToAdd" /> to the skip list.
        /// </summary>
        public void AddUser() {
            var user = this.UserToAdd;
            this.UserToAdd = null;
            if (string.IsNullOrWhiteSpace(user) || this.TwoListViewModel.RightList.Contains(user)) {
                return;
            }

            this.TwoListViewModel.AddRightList(user);
        }

        /// <summary>
        ///     Raised when the usernames to skip collection is changed.
        /// </summary>
        /// <param name="sender">The collection that changed.</param>
        /// <param name="e">The event arguments.</param>
        private void TtsSkipped_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            var current = Configuration.Instance.TtsUsernamesToSkip;
            if (null == current) {
                return;
            }

            var onlyNew = this.TwoListViewModel.RightList.Except(current).ToArray();
            var onlyOld = current.Except(this.TwoListViewModel.RightList).ToArray();
            current.RemoveMany(onlyOld);
            current.AddRange(onlyNew);

            Configuration.Instance.WriteConfiguration();
        }

        /// <summary>
        ///     Handles refreshing the user list.
        /// </summary>
        /// <param name="sender">The timer.</param>
        /// <param name="e">The event arguments.</param>
        private async void UserListRefreshTimer_OnElapsed(object sender, ElapsedEventArgs e) {
            var chatters = await TwitchChatManager.Instance.GetUsersFromAllChats();
            var set = new HashSet<string>(chatters.Select(c => c.Username));
            var onlyNew = set.Except(this.TwoListViewModel.LeftList).Except(this.TwoListViewModel.RightList).ToArray();
            var onlyOld = this.TwoListViewModel.LeftList.Except(set).ToArray();

            foreach (var oldItem in onlyOld) {
                this.TwoListViewModel.RemoveLeftList(oldItem);
            }

            foreach (var newItem in onlyNew) {
                this.TwoListViewModel.AddLeftList(newItem);
            }

            this.userListRefreshTimer.Interval = 5000;
            this.userListRefreshTimer.Start();
        }
    }
}