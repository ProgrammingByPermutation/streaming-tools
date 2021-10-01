namespace streaming_tools {
    using System.Collections.Generic;
    using Avalonia.Controls;
    using Avalonia.Input.Platform;
    using TwitchLib.Api.Core.Enums;

    /// <summary>
    ///     Global constants for use in the application.
    /// </summary>
    internal class Constants {
        /// <summary>
        ///     A regular expression for identifying a link.
        /// </summary>
        public const string REGEX_URL = @"(https?:\/\/(www\.)?)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=,]*)";

        /// <summary>
        ///     The client id of the twitch application.
        /// </summary>
        public const string NULLINSIDE_CLIENT_ID = @"7cpdxe88x0bjscf77hvglg9ebp0k3h";

        /// <summary>
        ///     The URL to redirect to after calling <see cref="NULLINSIDE_TWITCH_OAUTH" />.
        /// </summary>
        public const string NULLINSIDE_TWITCH_REDIRECT = @"https://www.nullinside.com/react/twitch_oauth";

        /// <summary>
        ///     The endpoint the refreshes an OAuth token.
        /// </summary>
        public const string NULLINSIDE_TWITCH_REFRESH = @"https://www.nullinside.com/api/v1/twitch/cathy-desktop/oauth/refresh";

        /// <summary>
        ///     The list of authorization scopes we need for the application.
        /// </summary>
        public static readonly IEnumerable<AuthScopes> TWITCH_AUTH_SCOPES = new[] { AuthScopes.Helix_Channel_Manage_Redemptions };

        /// <summary>
        ///     The twitch permissions to request. We just ask for everything since this runs on a local machine and we don't
        ///     give a fuck.
        /// </summary>
        public static readonly IEnumerable<string> TWITCH_SCOPES = new[] {
            "analytics:read:extensions",
            "analytics:read:games",
            "bits:read",
            "channel:edit:commercial",
            "channel:manage:broadcast",
            "channel:manage:extensions",
            "channel:manage:polls",
            "channel:manage:predictions",
            "channel:manage:redemptions",
            "channel:manage:schedule",
            "channel:manage:videos",
            "channel:read:editors",
            "channel:read:goals",
            "channel:read:hype_train",
            "channel:read:polls",
            "channel:read:predictions",
            "channel:read:redemptions",
            "channel:read:stream_key",
            "channel:read:subscriptions",
            "clips:edit",
            "moderation:read",
            "moderator:manage:automod",
            "user:edit",
            "user:edit:follows",
            "user:manage:blocked_users",
            "user:read:blocked_users",
            "user:read:broadcast",
            "user:read:email",
            "user:read:follows",
            "user:read:subscriptions",
            "channel_subscriptions",
            "channel_commercial",
            "channel_editor",
            "user_follows_edit",
            "channel_read",
            "user_read",
            "user_blocks_read",
            "user_blocks_edit",
            "channel:moderate",
            "chat:edit",
            "chat:read",
            "whispers:read",
            "whispers:edit"
        };

        /// <summary>
        ///     The reference to the main window of the application.
        /// </summary>
        /// <remarks>This is a hack for modal dialogs.</remarks>
        public static Window? MAIN_WINDOW;

        /// <summary>
        ///     The reference to the clipboard API.
        /// </summary>
        /// <remarks>This is a hack because it's hard to get to.</remarks>
        public static IClipboard? CLIPBOARD;

#if DEBUG
        /// <summary>
        ///     The hard coded location for where the keyboard hooks executable lives.
        /// </summary>
        public const string WINDOWS_KEYBOARD_HOOK_LOCATION = @"../../../../WindowsKeyboardHook/bin/Debug/net5.0/WindowsKeyboardHook.exe";
#else
        /// <summary>
        ///     The hard coded location for where the keyboard hooks executable lives.
        /// </summary>
        public const string WINDOWS_KEYBOARD_HOOK_LOCATION = @"../WindowsKeyboardHook/WindowsKeyboardHook.exe";
#endif
    }
}