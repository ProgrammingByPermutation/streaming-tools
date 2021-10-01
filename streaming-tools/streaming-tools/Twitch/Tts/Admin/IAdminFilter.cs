namespace streaming_tools.Twitch.Admin {
    using TwitchLib.Client;
    using TwitchLib.Client.Events;

    /// <summary>
    ///     Handles administration of the stream.
    /// </summary>
    public interface IAdminFilter {
        /// <summary>
        ///     Handles administration of the chat messages.
        /// </summary>
        /// <param name="config">The configuration for the twitch chat.</param>
        /// <param name="client">The twitch client.</param>
        /// <param name="messageInfo">The information on the chat message.</param>
        /// <returns>True if the message should be passed on, false if it should be discarded.</returns>
        bool Handle(TwitchChatConfiguration config, TwitchClient client, OnMessageReceivedArgs messageInfo);
    }
}