namespace streaming_tools.ViewModels {
    using System;
    using System.Collections.ObjectModel;
    using ReactiveUI;

    /// <summary>
    ///     Handles maintaining two lists and moving items between them.
    /// </summary>
    public class TwoListViewModel : ViewModelBase {
        /// <summary>
        ///     The behavior to maintain when double clicking
        /// </summary>
        public enum DoubleClickBehavior {
            /// <summary>
            ///     Move items from the list you double clicked on to the other list when the items are double clicked.
            /// </summary>
            MoveToOtherList,

            /// <summary>
            ///     Delete items from the list when double clicked.
            /// </summary>
            DeleteFromList
        }

        /// <summary>
        ///     The collection of items in the left list.
        /// </summary>
        private ObservableCollection<string> leftList;

        /// <summary>
        ///     The method to call when an item in the left list is double clicked.
        /// </summary>
        private Action<string?>? onLeftDoubleClick;

        /// <summary>
        ///     The method to call when an item in the right list is double clicked.
        /// </summary>
        private Action<string?>? onRightDoubleClick;

        /// <summary>
        ///     The collection of items in the right list.
        /// </summary>
        private ObservableCollection<string> rightList;

        /// <summary>
        ///     The behavior of how to handle double clicking on items in the right list.
        /// </summary>
        private DoubleClickBehavior rightListBehavior;

        /// <summary>
        ///     A value indicating whether the left list should be sorted.
        /// </summary>
        private bool sortLeftList;

        /// <summary>
        ///     A value indicating whether the right list should be sorted.
        /// </summary>
        private bool sortRightList;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TwoListViewModel" /> class.
        /// </summary>
        public TwoListViewModel() {
            this.leftList = new ObservableCollection<string>();
            this.rightList = new ObservableCollection<string>();
            this.OnLeftDoubleClick += this.OnLeftDoubleClicked;
            this.OnRightDoubleClick += this.OnRightDoubleClicked;
        }

        /// <summary>
        ///     Gets or sets the collection of items in the left list.
        /// </summary>
        public ObservableCollection<string> LeftList {
            get => this.leftList;
            set => this.RaiseAndSetIfChanged(ref this.leftList, value);
        }

        /// <summary>
        ///     Gets or sets the collection of items in the right list.
        /// </summary>
        public Action<string?>? OnLeftDoubleClick {
            get => this.onLeftDoubleClick;
            set => this.RaiseAndSetIfChanged(ref this.onLeftDoubleClick, value);
        }

        /// <summary>
        ///     Gets or sets the method to call when an item in the left list is double clicked.
        /// </summary>
        public Action<string?>? OnRightDoubleClick {
            get => this.onRightDoubleClick;
            set => this.RaiseAndSetIfChanged(ref this.onRightDoubleClick, value);
        }

        /// <summary>
        ///     Gets or sets the method to call when an item in the right list is double clicked.
        /// </summary>
        public ObservableCollection<string> RightList {
            get => this.rightList;
            set => this.RaiseAndSetIfChanged(ref this.rightList, value);
        }

        /// <summary>
        ///     Gets or sets the behavior of how to handle double clicking on items in the right list.
        /// </summary>
        public DoubleClickBehavior RightListBehavior {
            get => this.rightListBehavior;
            set => this.RaiseAndSetIfChanged(ref this.rightListBehavior, value);
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the left list should be sorted.
        /// </summary>
        public bool SortLeftList {
            get => this.sortLeftList;
            set => this.RaiseAndSetIfChanged(ref this.sortLeftList, value);
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the right list should be sorted.
        /// </summary>
        public bool SortRightList {
            get => this.sortRightList;
            set => this.RaiseAndSetIfChanged(ref this.sortRightList, value);
        }

        /// <summary>
        ///     Adds an item to the left list.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void AddLeftList(string item) {
            if (string.IsNullOrWhiteSpace(item)) {
                return;
            }

            if (!this.SortLeftList) {
                this.LeftList.Add(item);
                return;
            }

            this.AddToList(this.LeftList, item);
        }

        /// <summary>
        ///     Adds an item to the right list.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void AddRightList(string item) {
            if (!this.SortRightList) {
                this.RightList.Add(item);
                return;
            }

            this.AddToList(this.RightList, item);
        }

        /// <summary>
        ///     Removes an item from the left list.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        public void RemoveLeftList(string item) {
            this.LeftList.Remove(item);
        }

        /// <summary>
        ///     Removes an item from the right list.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        public void RemoveRightList(string item) {
            this.RightList.Remove(item);
        }

        /// <summary>
        ///     Moves items from the left list to the right list.
        /// </summary>
        /// <param name="selectedItem">The item to move.</param>
        protected virtual void OnLeftDoubleClicked(string? selectedItem) {
            if (string.IsNullOrWhiteSpace(selectedItem)) {
                return;
            }

            this.LeftList.Remove(selectedItem);
            this.AddRightList(selectedItem);
        }

        /// <summary>
        ///     Moves items from the right list to the left list.
        /// </summary>
        /// <param name="selectedItem">The item to move.</param>
        protected virtual void OnRightDoubleClicked(string? selectedItem) {
            if (string.IsNullOrWhiteSpace(selectedItem)) {
                return;
            }

            this.RightList.Remove(selectedItem);

            if (DoubleClickBehavior.MoveToOtherList == this.RightListBehavior) {
                this.AddLeftList(selectedItem);
            }
        }

        /// <summary>
        ///     Adds an item to the provided collection.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="item">The item to add.</param>
        private void AddToList(ObservableCollection<string> collection, string item) {
            if (string.IsNullOrWhiteSpace(item)) {
                return;
            }

            if (0 == collection.Count) {
                collection.Add(item);
                return;
            }

            for (var i = 0; i < collection.Count; i++) {
                var comp = string.Compare(item, collection[i], StringComparison.InvariantCultureIgnoreCase);
                if (0 == comp || 0 > comp) {
                    collection.Insert(i, item);
                    return;
                }
            }

            collection.Add(item);
        }
    }
}