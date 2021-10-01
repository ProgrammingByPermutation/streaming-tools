namespace streaming_tools.ViewModels {
    using System;

    /// <summary>
    ///     The UI representation of a twitch account.
    /// </summary>
    public class AccountViewModel : ViewModelBase {
        /// <summary>
        ///     Gets or sets the delegate provided from the parent control when deleting an account.
        /// </summary>
        public Action? DeleteAccount { get; set; }

        /// <summary>
        ///     Gets or sets the delegate provided from the parent control when editing an account.
        /// </summary>
        public Action? EditAccount { get; set; }

        /// <summary>
        ///     Gets or sets the username of the twitch account.
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        ///     Handles executing the delete account action.
        /// </summary>
        public void DeleteAccountCommand() {
            this.DeleteAccount?.Invoke();
        }

        /// <summary>
        ///     Handles executing the edit account action.
        /// </summary>
        public void EditAccountCommand() {
            this.EditAccount?.Invoke();
        }
    }
}