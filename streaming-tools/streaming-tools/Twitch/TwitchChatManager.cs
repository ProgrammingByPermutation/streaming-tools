namespace streaming_tools.Twitch {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;
    using Models;
    using Newtonsoft.Json;
    using TwitchLib.Api;
    using TwitchLib.Api.Core.Enums;
    using TwitchLib.Api.Helix.Models.Users.GetUsers;
    using TwitchLib.Client;
    using TwitchLib.Client.Events;
    using TwitchLib.Client.Extensions;
    using TwitchLib.Client.Models;
    using TwitchLib.Communication.Clients;
    using TwitchLib.Communication.Models;
    using TwitchLib.PubSub;
    using TwitchLib.PubSub.Events;
    using Timer = System.Timers.Timer;

    /// <summary>
    ///     Organizes and aggregates the clients connected to zero or more twitch chats. Invokes callbacks for messages
    ///     received in chats.
    /// </summary>
    public class TwitchChatManager {
        #region Fields

        /// <summary>
        ///     The singleton instance of the class.
        /// </summary>
        private static TwitchChatManager? instance;

        /// <summary>
        ///     The timer responsible for reconnecting twitch chats.
        /// </summary>
        private readonly Timer reconnectTimer;

        /// <summary>
        ///     The mapping of twitch clients to their requested configurations.
        /// </summary>
        private readonly Dictionary<TwitchClient, TwitchConnection?> twitchClients = new();

        /// <summary>
        ///     The publish subscribe interface from Twitch for listening to follows and channel point redemptions.
        /// </summary>
        private readonly TwitchPubSub pubSub;

        #endregion

        #region Constructors/Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="TwitchChatManager" /> class.
        /// </summary>
        /// <remarks>This is protected to prevent instantiation outside of our singleton.</remarks>
        protected TwitchChatManager() {
            // Setup the pub sub twitch api
            this.pubSub = new TwitchPubSub();
            this.pubSub.OnRewardRedeemed += this.PubSubOnRewardRedeemed;
            this.pubSub.OnFollow += this.PubSubOnFollow;
            this.pubSub.OnPubSubServiceConnected += this.PubSubOnPubSubServiceConnected;
            this.pubSub.Connect();

            // A timer for checking if the twitch client is connected.
            this.reconnectTimer = new Timer(1000);
            this.reconnectTimer.AutoReset = false;
            this.reconnectTimer.Elapsed += this.ReconnectTimer_OnElapsed;
            this.reconnectTimer.Start();
        }

        /// <summary>
        ///     Gets the singleton instance of the class.
        /// </summary>
        public static TwitchChatManager Instance {
            get {
                if (null == TwitchChatManager.instance) {
                    TwitchChatManager.instance = new TwitchChatManager();
                }

                return TwitchChatManager.instance;
            }
        }

        #endregion

        #region Twitch Event Subscriptions

        /// <summary>
        ///     Adds a callback from receiving twitch chat messages.
        /// </summary>
        /// <param name="account">The account to connect with.</param>
        /// <param name="channel">The name of the channel to join.</param>
        /// <param name="messageCallback">The callback to invoke when a message is received.</param>
        public async void AddTwitchChannel(TwitchAccount? account, string? channel, Action<TwitchClient, OnMessageReceivedArgs>? messageCallback) {
            if (null == account || null == channel || null == messageCallback) {
                return;
            }

            var conn = await this.GetOrCreateConnection(account, channel);
            if (null == conn || null == conn.Item2) {
                return;
            }

            conn.Item2.MessageCallbacks += messageCallback;
        }

        /// <summary>
        ///     Remove a callback from receiving twitch chat messages.
        /// </summary>
        /// <param name="account">The account originally subscribed with.</param>
        /// <param name="channel">The name of the channel that was joined.</param>
        /// <param name="messageCallback">The callback to remove.</param>
        public async void RemoveTwitchChannel(TwitchAccount? account, string? channel, Action<TwitchClient, OnMessageReceivedArgs>? messageCallback) {
            if (null == account || null == channel || null == messageCallback) {
                return;
            }

            TwitchClient? removeClient = null;
            lock (this.twitchClients) {
                var allExisting = from connection in this.twitchClients
                    where connection.Value.Account == account && connection.Value.Channel?.Equals(channel, StringComparison.InvariantCultureIgnoreCase) == true
                    select connection;

                foreach (var existing in allExisting.ToArray()) {
                    if (null == existing.Value) {
                        continue;
                    }

                    existing.Value.MessageCallbacks -= messageCallback;

                    if (this.HasNoCallbacks(existing.Value)) {
                        this.twitchClients.Remove(existing.Key);
                        removeClient = existing.Key;
                    }
                }
            }

            if (null != removeClient) {
                await Task.Run(() => removeClient.Disconnect());
            }
        }

        /// <summary>
        ///     Adds a callback to perform administrative functions on twitch chat (e.g. like banning users) and optionally
        ///     preventing messages from being propagated to other callbacks.
        /// </summary>
        /// <param name="account">The account to connect with.</param>
        /// <param name="channel">The name of the channel to join.</param>
        /// <param name="adminCallback">The callback to invoke when a message is received.</param>
        public async void AddTwitchChannelAdminFilter(TwitchAccount? account, string? channel, Func<TwitchClient, OnMessageReceivedArgs, bool>? adminCallback) {
            if (null == account || null == channel || null == adminCallback) {
                return;
            }

            var conn = await this.GetOrCreateConnection(account, channel);
            if (null == conn || null == conn.Item2) {
                return;
            }

            conn.Item2.AdminCallbacks += adminCallback;
        }

        /// <summary>
        ///     Remove a callback from administering twitch chat.
        /// </summary>
        /// <param name="account">The account originally subscribed with.</param>
        /// <param name="channel">The name of the channel that was joined.</param>
        /// <param name="adminCallback">The callback to remove.</param>
        public async void RemoveTwitchChannelAdminFilter(TwitchAccount? account, string? channel, Func<TwitchClient, OnMessageReceivedArgs, bool>? adminCallback) {
            if (null == account || null == channel || null == adminCallback) {
                return;
            }

            TwitchClient? removeClient = null;
            lock (this.twitchClients) {
                var allExisting = from connection in this.twitchClients
                    where connection.Value.Account == account && connection.Value.Channel?.Equals(channel, StringComparison.InvariantCultureIgnoreCase) == true
                    select connection;

                foreach (var existing in allExisting.ToArray()) {
                    if (null == existing.Value) {
                        continue;
                    }

                    if (null != existing.Value.AdminCallbacks) {
                        existing.Value.AdminCallbacks -= adminCallback;
                    }

                    if (this.HasNoCallbacks(existing.Value)) {
                        this.twitchClients.Remove(existing.Key);
                        removeClient = existing.Key;
                    }
                }
            }

            if (null != removeClient) {
                await Task.Run(() => removeClient.Disconnect());
            }
        }

        /// <summary>
        ///     Adds a callback for when channel points are redeemed.
        /// </summary>
        /// <param name="account">The twitch account to register callback with.</param>
        /// <param name="channel">The twitch channel to get callbacks for.</param>
        /// <param name="messageCallback">The callback to invoke.</param>
        public async void AddChannelPointsCallback(TwitchAccount? account, string? channel, Action<TwitchClient, OnRewardRedeemedArgs>? messageCallback) {
            if (string.IsNullOrWhiteSpace(account?.Username) || string.IsNullOrWhiteSpace(channel)) {
                return;
            }

            var user = await this.TryGetUser(account, channel);
            if (null == user) {
                return;
            }

            var conn = await this.GetOrCreateConnection(account, channel);
            if (null == conn || null == conn.Item2) {
                return;
            }

            conn.Item2.OnChannelPointRedeem += messageCallback;
            this.pubSub.ListenToRewards(user.Id);
            this.SendPubSubTopics();
        }

        /// <summary>
        ///     Removes a callback from when channel points are redeemed.
        /// </summary>
        /// <param name="account">The twitch account to register callback with.</param>
        /// <param name="channel">The twitch channel to get callbacks for.</param>
        /// <param name="messageCallback">The callback to remove.</param>
        public async void RemoveChannelPointsCallback(TwitchAccount? account, string? channel, Action<TwitchClient, OnRewardRedeemedArgs>? messageCallback) {
            if (string.IsNullOrWhiteSpace(account?.Username) || string.IsNullOrWhiteSpace(channel)) {
                return;
            }

            TwitchClient? removeClient = null;
            lock (this.twitchClients) {
                var allExisting = from connection in this.twitchClients
                    where connection.Value.Account == account && connection.Value.Channel?.Equals(channel, StringComparison.InvariantCultureIgnoreCase) == true
                    select connection;

                foreach (var existing in allExisting.ToArray()) {
                    if (null == existing.Value) {
                        continue;
                    }

                    if (null != existing.Value.OnChannelPointRedeem) {
                        existing.Value.OnChannelPointRedeem -= messageCallback;
                    }

                    if (this.HasNoCallbacks(existing.Value)) {
                        this.twitchClients.Remove(existing.Key);
                    }
                }
            }

            if (null != removeClient) {
                await Task.Run(() => removeClient.Disconnect());
            }
        }

        /// <summary>
        ///     Adds a callback to notify when someone follows.
        /// </summary>
        /// <param name="account">The twitch account to register callback with.</param>
        /// <param name="channel">The twitch channel to get callbacks for.</param>
        /// <param name="messageCallback">The callback to invoke.</param>
        public async void AddFollowCallback(TwitchAccount? account, string? channel, Action<TwitchClient, OnFollowArgs>? messageCallback) {
            if (string.IsNullOrWhiteSpace(account?.Username) || string.IsNullOrWhiteSpace(channel)) {
                return;
            }

            var user = await this.TryGetUser(account, channel);
            if (null == user) {
                return;
            }

            var conn = await this.GetOrCreateConnection(account, channel);
            if (null == conn || null == conn.Item2) {
                return;
            }

            conn.Item2.OnFollow += messageCallback;
            this.pubSub.ListenToFollows(user.Id);
            this.SendPubSubTopics();
        }

        /// <summary>
        ///     Removes a callback to notify when someone follows.
        /// </summary>
        /// <param name="account">The twitch account to register callback with.</param>
        /// <param name="channel">The twitch channel to get callbacks for.</param>
        /// <param name="messageCallback">The callback to remove.</param>
        public async void RemoveFollowCallback(TwitchAccount? account, string? channel, Action<TwitchClient, OnFollowArgs>? messageCallback) {
            if (string.IsNullOrWhiteSpace(account?.Username) || string.IsNullOrWhiteSpace(channel)) {
                return;
            }

            TwitchClient? removeClient = null;
            lock (this.twitchClients) {
                var allExisting = from connection in this.twitchClients
                    where connection.Value.Account == account && connection.Value.Channel?.Equals(channel, StringComparison.InvariantCultureIgnoreCase) == true
                    select connection;

                foreach (var existing in allExisting.ToArray()) {
                    if (null == existing.Value) {
                        continue;
                    }

                    if (null != existing.Value.OnFollow) {
                        existing.Value.OnFollow -= messageCallback;
                    }

                    if (this.HasNoCallbacks(existing.Value)) {
                        this.twitchClients.Remove(existing.Key);
                    }
                }
            }

            if (null != removeClient) {
                await Task.Run(() => removeClient.Disconnect());
            }
        }

        /// <summary>
        ///     Adds a callback to notify when someone hosts a stream.
        /// </summary>
        /// <param name="account">The twitch account to register callback with.</param>
        /// <param name="channel">The twitch channel to get callbacks for.</param>
        /// <param name="messageCallback">The callback to invoke.</param>
        public async void AddHostCallback(TwitchAccount? account, string? channel, Action<TwitchClient, OnBeingHostedArgs>? messageCallback) {
            if (string.IsNullOrWhiteSpace(account?.Username) || string.IsNullOrWhiteSpace(channel)) {
                return;
            }

            var user = await this.TryGetUser(account, channel);
            if (null == user) {
                return;
            }

            var conn = await this.GetOrCreateConnection(account, channel);
            if (null == conn || null == conn.Item2) {
                return;
            }

            conn.Item2.OnHost += messageCallback;
        }

        /// <summary>
        ///     Removes a callback to notify when someone hosts a stream.
        /// </summary>
        /// <param name="account">The twitch account to register callback with.</param>
        /// <param name="channel">The twitch channel to get callbacks for.</param>
        /// <param name="messageCallback">The callback to remove.</param>
        public async void RemoveHostCallback(TwitchAccount? account, string? channel, Action<TwitchClient, OnBeingHostedArgs>? messageCallback) {
            if (string.IsNullOrWhiteSpace(account?.Username) || string.IsNullOrWhiteSpace(channel)) {
                return;
            }

            TwitchClient? removeClient = null;
            lock (this.twitchClients) {
                var allExisting = from connection in this.twitchClients
                    where connection.Value.Account == account && connection.Value.Channel?.Equals(channel, StringComparison.InvariantCultureIgnoreCase) == true
                    select connection;

                foreach (var existing in allExisting.ToArray()) {
                    if (null == existing.Value) {
                        continue;
                    }

                    if (null != existing.Value.OnHost) {
                        existing.Value.OnHost -= messageCallback;
                    }

                    if (this.HasNoCallbacks(existing.Value)) {
                        this.twitchClients.Remove(existing.Key);
                    }
                }
            }

            if (null != removeClient) {
                await Task.Run(() => removeClient.Disconnect());
            }
        }

        /// <summary>
        ///     Adds a callback to notify when someone raids a stream.
        /// </summary>
        /// <param name="account">The twitch account to register callback with.</param>
        /// <param name="channel">The twitch channel to get callbacks for.</param>
        /// <param name="messageCallback">The callback to invoke.</param>
        public async void AddRaidCallback(TwitchAccount? account, string? channel, Action<TwitchClient, OnRaidNotificationArgs>? messageCallback) {
            if (string.IsNullOrWhiteSpace(account?.Username) || string.IsNullOrWhiteSpace(channel)) {
                return;
            }

            var user = await this.TryGetUser(account, channel);
            if (null == user) {
                return;
            }

            var conn = await this.GetOrCreateConnection(account, channel);
            if (null == conn || null == conn.Item2) {
                return;
            }

            conn.Item2.OnRaid += messageCallback;
        }

        /// <summary>
        ///     Removes a callback to notify when someone raids a stream.
        /// </summary>
        /// <param name="account">The twitch account to register callback with.</param>
        /// <param name="channel">The twitch channel to get callbacks for.</param>
        /// <param name="messageCallback">The callback to remove.</param>
        public async void RemoveRaidCallback(TwitchAccount? account, string? channel, Action<TwitchClient, OnRaidNotificationArgs>? messageCallback) {
            if (string.IsNullOrWhiteSpace(account?.Username) || string.IsNullOrWhiteSpace(channel)) {
                return;
            }

            TwitchClient? removeClient = null;
            lock (this.twitchClients) {
                var allExisting = from connection in this.twitchClients
                    where connection.Value.Account == account && connection.Value.Channel?.Equals(channel, StringComparison.InvariantCultureIgnoreCase) == true
                    select connection;

                foreach (var existing in allExisting.ToArray()) {
                    if (null == existing.Value) {
                        continue;
                    }

                    if (null != existing.Value.OnRaid) {
                        existing.Value.OnRaid -= messageCallback;
                    }

                    if (this.HasNoCallbacks(existing.Value)) {
                        this.twitchClients.Remove(existing.Key);
                    }
                }
            }

            if (null != removeClient) {
                await Task.Run(() => removeClient.Disconnect());
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Gets the twitch client connected to the specified channel.
        /// </summary>
        /// <param name="channel">The twitch channel that we are connected to.</param>
        /// <returns>The twitch client if a connection exists, null otherwise.</returns>
        public TwitchClient? GetTwitchChannelClient(string channel) {
            lock (this.twitchClients) {
                var existing = from connection in this.twitchClients
                    where connection.Value.Channel?.Equals(channel, StringComparison.InvariantCultureIgnoreCase) == true
                    select connection;

                var pair = existing.FirstOrDefault();
                if (default(KeyValuePair<TwitchClient, TwitchConnection>).Equals(pair)) {
                    return null;
                }

                return pair.Key;
            }
        }

        /// <summary>
        ///     Gets the twitch client connected to the specified channel.
        /// </summary>
        /// <param name="username">The twitch username that is connected.</param>
        /// <returns>The twitch client if a connection exists, null otherwise.</returns>
        public async Task<TwitchAPI?> GetTwitchClientApi(string username) {
            var account = Configuration.Instance.TwitchAccounts?.FirstOrDefault(a => username.Equals(a.Username, StringComparison.InvariantCultureIgnoreCase));
            if (null == account || string.IsNullOrWhiteSpace(account.Username) || string.IsNullOrWhiteSpace(account.ApiOAuth)) {
                return null;
            }

            var api = new TwitchAPI {
                Settings = {
                    ClientId = Constants.NULLINSIDE_CLIENT_ID,
                    Scopes = new List<AuthScopes>(Constants.TWITCH_AUTH_SCOPES)
                }
            };

            if (account.ApiOAuthExpires <= DateTime.UtcNow && null != account.ApiOAuthRefresh) {
                try {
                    var refreshToken = Encoding.UTF8.GetString(Convert.FromBase64String(account.ApiOAuthRefresh));
                    var client = new HttpClient();
                    var nullinsideResponse = await client.PostAsync($"{Constants.NULLINSIDE_TWITCH_REFRESH}?refresh_token={refreshToken}", new StringContent(""));
                    if (!nullinsideResponse.IsSuccessStatusCode) {
                        return null;
                    }

                    var responseString = await nullinsideResponse.Content.ReadAsStringAsync();
                    var json = JsonConvert.DeserializeObject<TwitchTokenResponseJson>(responseString);
                    if (null == json) {
                        return api;
                    }

                    account.ApiOAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes(json.access_token));
                    account.ApiOAuthRefresh = Convert.ToBase64String(Encoding.UTF8.GetBytes(json.refresh_token));
                    account.ApiOAuthExpires = DateTime.UtcNow + new TimeSpan(0, 0, json.expires_in - 300);
                    Configuration.Instance.WriteConfiguration();
                } catch (Exception) { }
            }

            api.Settings.AccessToken = Encoding.UTF8.GetString(Convert.FromBase64String(account.ApiOAuth));
            return api;
        }

        /// <summary>
        ///     Retrieves all users from all currently connected chats.
        /// </summary>
        /// <returns>A collection of currently existing users in chat.</returns>
        public async Task<TwitchChatter[]> GetUsersFromAllChats() {
            var usernameChannels = new List<Tuple<string, string>>();
            lock (this.twitchClients) {
                foreach (var connection in this.twitchClients.Values) {
                    if (null == connection?.Account?.Username || null == connection.Channel) {
                        continue;
                    }

                    // False positive
#pragma warning disable CS8604
                    usernameChannels.Add(new Tuple<string, string>(connection.Channel, connection.Account?.Username));
#pragma warning restore CS8604
                }
            }

            var tasks = usernameChannels.Select(tuple => this.GetUsersFromChat(tuple.Item1, tuple.Item2));
            var allChatters = new List<TwitchChatter>();
            foreach (var chatters in await Task.WhenAll(tasks)) {
                if (null == chatters) {
                    continue;
                }

                allChatters.AddRange(chatters);
            }

            return allChatters.ToArray();
        }

        /// <summary>
        ///     Gets whether a twitch user to connected to a twitch channel.
        /// </summary>
        /// <param name="username">The user that we want to check.</param>
        /// <param name="channel">The channel they're connected to.</param>
        /// <returns>True if connected, false otherwise.</returns>
        public bool TwitchChannelIsConnected(string username, string channel) {
            lock (this.twitchClients) {
                var allExisting = from connection in this.twitchClients
                    where username.Equals(connection.Value.Account?.Username) && connection.Value.Channel?.Equals(channel, StringComparison.InvariantCultureIgnoreCase) == true
                    select connection;

                if (!allExisting.Any()) {
                    return false;
                }

                var conn = allExisting.FirstOrDefault().Key;
                return conn.IsConnected && conn.JoinedChannels.Count > 0;
            }
        }

        #endregion

        #region PubSub

        /// <summary>
        ///     Handles sending topics once the pub sub service is connected to.
        /// </summary>
        /// <param name="sender">The pub sub service.</param>
        /// <param name="e">The event arguments.</param>
        private void PubSubOnPubSubServiceConnected(object? sender, EventArgs e) {
            this.SendPubSubTopics();
        }

        /// <summary>
        ///     Invoked when a new user follows from any of the channels we are monitoring.
        /// </summary>
        /// <param name="sender">The pub sub service.</param>
        /// <param name="e">The event arguments.</param>
        private void PubSubOnFollow(object? sender, OnFollowArgs e) {
            KeyValuePair<TwitchClient, TwitchConnection?>? channelInfo = null;

            lock (this.twitchClients) {
                channelInfo = this.twitchClients.FirstOrDefault(c => c.Value?.Channel?.Equals(e.Username, StringComparison.InvariantCultureIgnoreCase) ?? false);
            }

            if (!channelInfo.HasValue || null == channelInfo.Value.Value?.OnFollow) {
                return;
            }

            foreach (var callback in channelInfo.Value.Value.OnFollow.GetInvocationList()) {
                try {
                    callback.DynamicInvoke(channelInfo.Value.Key, e);
                } catch { }
            }
        }

        /// <summary>
        ///     Invoked when channel points are redeemed in any of the channels we are monitoring.
        /// </summary>
        /// <param name="sender">The pub sub service.</param>
        /// <param name="e">The event arguments.</param>
        private void PubSubOnRewardRedeemed(object? sender, OnRewardRedeemedArgs e) {
            IEnumerable<KeyValuePair<TwitchClient, TwitchConnection?>>? channelInfos = null;

            lock (this.twitchClients) {
                channelInfos = this.twitchClients.Where(c => c.Value?.ChannelId?.Equals(e.ChannelId, StringComparison.InvariantCultureIgnoreCase) ?? false);
            }

            foreach (var channelInfo in channelInfos) {
                if (null == channelInfo.Value?.OnChannelPointRedeem) {
                    continue;
                }

                foreach (var callback in channelInfo.Value.OnChannelPointRedeem.GetInvocationList()) {
                    try {
                        callback.DynamicInvoke(channelInfo.Key, e);
                    } catch { }
                }
            }
        }

        /// <summary>
        ///     Sends pub sub topics across all OAuth tokens.
        /// </summary>
        private void SendPubSubTopics() {
            if (null == Configuration.Instance.TwitchAccounts) {
                return;
            }

            foreach (var account in Configuration.Instance.TwitchAccounts) {
                if (string.IsNullOrWhiteSpace(account.ApiOAuth)) {
                    continue;
                }

                this.pubSub?.SendTopics(account.ApiOAuth);
            }
        }

        #endregion

        #region Twitch Client Callbacks

        /// <summary>
        ///     Automatically shouts out a channel that hosts us.
        /// </summary>
        /// <param name="sender">The twitch client.</param>
        /// <param name="e">The host information.</param>
        private void TwitchClient_OnBeingHosted(object? sender, OnBeingHostedArgs e) {
            var twitchClient = sender as TwitchClient;
            if (null == twitchClient) {
                return;
            }

            TwitchConnection? connection;
            lock (this.twitchClients) {
                connection = this.twitchClients.Values.FirstOrDefault(c => c?.Channel?.Equals(e.BeingHostedNotification.Channel, StringComparison.InvariantCultureIgnoreCase) ?? false);
            }

            if (null == connection?.OnHost) {
                return;
            }

            foreach (var callback in connection.OnHost.GetInvocationList()) {
                try {
                    callback.DynamicInvoke(twitchClient, e);
                } catch { }
            }

            // twitchClient.SendMessage(e.BeingHostedNotification.Channel, $"!so {e.BeingHostedNotification.HostedByChannel}");
        }

        /// <summary>
        ///     Automatically shouts out a channel that raids us.
        /// </summary>
        /// <param name="sender">The twitch client.</param>
        /// <param name="e">The raid information.</param>
        private void TwitchClient_OnRaidNotification(object? sender, OnRaidNotificationArgs e) {
            var twitchClient = sender as TwitchClient;
            if (null == twitchClient) {
                return;
            }

            TwitchConnection? connection;
            lock (this.twitchClients) {
                connection = this.twitchClients.Values.FirstOrDefault(c => c?.Channel?.Equals(e.Channel, StringComparison.InvariantCultureIgnoreCase) ?? false);
            }

            if (null == connection?.OnRaid) {
                return;
            }

            foreach (var callback in connection.OnRaid.GetInvocationList()) {
                try {
                    callback.DynamicInvoke(twitchClient, e);
                } catch { }
            }

            // twitchClient.SendMessage(e.Channel, $"!so {e.RaidNotification.DisplayName}");
        }

        /// <summary>
        ///     The callback invoked when a message is received in twitch chat.
        /// </summary>
        /// <param name="sender">The twitch chat client.</param>
        /// <param name="e">The message information.</param>
        private void TwitchClient_OnMessageReceived(object? sender, OnMessageReceivedArgs e) {
            if (null == sender) {
                return;
            }

            var twitchClient = sender as TwitchClient;
            if (null == twitchClient) {
                return;
            }

            TwitchConnection? conn;
            lock (this.twitchClients) {
                conn = this.twitchClients.GetValueOrDefault(twitchClient, null);
            }

            if (null == conn) {
                return;
            }

            if (null != conn.AdminCallbacks) {
                foreach (var adminFilter in conn.AdminCallbacks.GetInvocationList()) {
                    var shouldContinue = (bool)(adminFilter.DynamicInvoke(twitchClient, e) ?? true);
                    if (!shouldContinue) {
                        return;
                    }
                }
            }

            conn.MessageCallbacks?.Invoke(twitchClient, e);
        }

        /// <summary>
        ///     Determines if chats are connected and reconnects if they are not.
        /// </summary>
        /// <param name="sender">The timer.</param>
        /// <param name="e">The event arguments.</param>
        private async void ReconnectTimer_OnElapsed(object sender, ElapsedEventArgs e) {
            try {
                // This is a race condition but we're going with it for now. We will grab the entire list of clients
                // and loop through for information and connecting outside of the lock. Technically we could be removing
                // something at the same time as we're trying to reconnect to them.
                KeyValuePair<TwitchClient, TwitchConnection?>[] clients;
                lock (this.twitchClients) {
                    clients = this.twitchClients.ToArray();
                }

                foreach (var client in clients) {
                    if (!client.Key.IsInitialized) {
                        continue;
                    }

                    // If the connection is established, perform a reconnection.
                    if (!client.Key.IsConnected) {
                        await Task.WhenAny(Task.Run(() => {
                            try {
                                using (var signal = new ManualResetEventSlim(false)) {
                                    var waiting = new EventHandler<OnConnectedArgs>((o, args) => { signal.Set(); });

                                    client.Key.OnConnected += waiting;
                                    client.Key.Reconnect();
                                    signal.Wait(10000);
                                    client.Key.OnConnected -= waiting;
                                }
                            } catch (Exception) { }
                        }), Task.Delay(15000));
                    }

                    // If we are connected but we are not actually in the twitch chat channel like
                    // we're supposed to be, join the channel. I don't know why sometimes we lose
                    // the joined channel completely after being connected but we do.
                    if (0 == client.Key.JoinedChannels.Count) {
                        await Task.WhenAny(Task.Run(() => {
                            try {
                                if (null == client.Value) {
                                    return;
                                }

                                using (var signal = new ManualResetEventSlim(false)) {
                                    var waiting = new EventHandler<OnJoinedChannelArgs>((o, args) => { signal.Set(); });

                                    client.Key.OnJoinedChannel += waiting;
                                    client.Key.JoinChannel(client.Value.Channel);
                                    signal.Wait(10000);
                                    client.Key.OnJoinedChannel -= waiting;
                                }
                            } catch (Exception) { }
                        }), Task.Delay(15000));
                    }
                }
            } finally {
                this.reconnectTimer.Start();
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        ///     Attempts to get the API user object for a username.
        /// </summary>
        /// <param name="account">The account to use for looking up the user.</param>
        /// <param name="user">The username to lookup.</param>
        /// <returns>The <seealso cref="User" /> if successful, null otherwise.</returns>
        private async Task<User?> TryGetUser(TwitchAccount? account, string? user) {
            if (string.IsNullOrWhiteSpace(account?.Username) || string.IsNullOrWhiteSpace(user)) {
                return null;
            }

            var api = await this.GetTwitchClientApi(account.Username);
            if (null == api) {
                return null;
            }

            var response = await api.Helix.Users.GetUsersAsync(logins: new List<string> { user });
            if (null == response) {
                return null;
            }

            return response.Users.FirstOrDefault();
        }

        /// <summary>
        ///     Gets or creates a new connection to a twitch chat.
        /// </summary>
        /// <param name="account">The account to connect with.</param>
        /// <param name="channel">The twitch channel to connect to.</param>
        /// <returns>An instance of the twitch connection.</returns>
        private async Task<Tuple<TwitchClient, TwitchConnection?>?> GetOrCreateConnection(TwitchAccount account, string channel) {
            if (string.IsNullOrWhiteSpace(account.Username)) {
                return null;
            }

            // Return the global connection.
            TwitchConnection conn;
            TwitchClient twitchClient;
            lock (this.twitchClients) {
                var existing = from connection in this.twitchClients
                    where connection.Value.Account == account && connection.Value.Channel?.Equals(channel, StringComparison.InvariantCultureIgnoreCase) == true
                    select connection;

                if (existing.Any()) {
                    var pair = existing.First();
                    return new Tuple<TwitchClient, TwitchConnection?>(pair.Key, pair.Value);
                }

                // Create a new connection
                conn = new TwitchConnection { Account = account, Channel = channel };
                var clientOptions = new ClientOptions { MessagesAllowedInPeriod = 750, ThrottlingPeriod = TimeSpan.FromSeconds(30) };

                WebSocketClient customClient = new(clientOptions);
                twitchClient = new TwitchClient(customClient);

                this.twitchClients[twitchClient] = conn;
            }

            var api = await this.GetTwitchClientApi(account.Username);
            if (null == api) {
                return null;
            }

            var users = await api.Helix.Users.GetUsersAsync(logins: new List<string>(new[] { channel }));
            if (null == users) {
                return null;
            }

            conn.ChannelId = users.Users.FirstOrDefault()?.Id;

            // Run the connecting asynchronously
            await Task.Run(() => {
                try {
                    var password = null != account.ApiOAuth ? Encoding.UTF8.GetString(Convert.FromBase64String(account.ApiOAuth)) : null;
                    var credentials = new ConnectionCredentials(account.Username, password ?? "");

                    twitchClient.Initialize(credentials, channel);
                    twitchClient.AutoReListenOnException = true;
                    twitchClient.OnMessageReceived += this.TwitchClient_OnMessageReceived;
                    twitchClient.OnBeingHosted += this.TwitchClient_OnBeingHosted;
                    twitchClient.OnRaidNotification += this.TwitchClient_OnRaidNotification;
                } catch (Exception) { }
            });

            return new Tuple<TwitchClient, TwitchConnection?>(twitchClient, conn);
        }

        /// <summary>
        ///     Gets all of the users from a twitch connection.
        /// </summary>
        /// <param name="channel">The channel to check for users.</param>
        /// <param name="username">The username to user to check.</param>
        /// <returns>A collection of currently existing users in chat.</returns>
        private async Task<ICollection<TwitchChatter>?> GetUsersFromChat(string channel, string username) {
            var api = await this.GetTwitchClientApi(username);
            if (null == api) {
                return null;
            }

            try {
                var resp = await api.Undocumented.GetChattersAsync(channel);
                return resp.Select(c => new TwitchChatter(channel, c.Username)).ToArray();
            } catch (Exception) {
                return null;
            }
        }

        /// <summary>
        ///     Checks a twitch connection for callbacks.
        /// </summary>
        /// <param name="conn">The connection to check.</param>
        /// <returns>True if there are no callbacks on a twitch client, false otherwise.</returns>
        private bool HasNoCallbacks(TwitchConnection conn) {
            return null == conn.AdminCallbacks && null == conn.MessageCallbacks && null == conn.OnFollow &&
                   null == conn.OnHost && null == conn.OnRaid && null == conn.OnChannelPointRedeem;
        }

        #endregion

        #region Helper Classes

        /// <summary>
        ///     Represents a twitch chatter in a channel.
        /// </summary>
        public struct TwitchChatter {
            /// <summary>
            ///     The channel the user is in.
            /// </summary>
            public string Channel;

            /// <summary>
            ///     The username of the user.
            /// </summary>
            public string Username;

            /// <summary>
            ///     Initializes a new instance of the <see cref="TwitchChatter" /> struct.
            /// </summary>
            /// <param name="channel">The channel the user is in.</param>
            /// <param name="username">The username of the user.</param>
            public TwitchChatter(string channel, string username) {
                this.Channel = channel;
                this.Username = username;
            }
        }

        /// <summary>
        ///     A mapping of all information related to a single twitch chat connection.
        /// </summary>
        private class TwitchConnection {
            /// <summary>
            ///     Gets or sets the account connected with.
            /// </summary>
            public TwitchAccount? Account { get; set; }

            /// <summary>
            ///     Gets or sets the callbacks used to administrate the twitch chat.
            /// </summary>
            public Func<TwitchClient, OnMessageReceivedArgs, bool>? AdminCallbacks { get; set; }

            /// <summary>
            ///     Gets or sets the callbacks invoked when channel points are redeemed.
            /// </summary>
            public Action<TwitchClient, OnRewardRedeemedArgs>? OnChannelPointRedeem { get; set; }

            /// <summary>
            ///     Gets or sets the callbacks invoked when a user follows a channel.
            /// </summary>
            public Action<TwitchClient, OnFollowArgs>? OnFollow { get; set; }

            /// <summary>
            ///     Gets or sets the callback invoked when a user hosts a channel.
            /// </summary>
            public Action<TwitchClient, OnBeingHostedArgs>? OnHost { get; set; }

            /// <summary>
            ///     Gets or sets the callback invoked when a user raids a channel.
            /// </summary>
            public Action<TwitchClient, OnRaidNotificationArgs>? OnRaid { get; set; }

            /// <summary>
            ///     Gets or sets the channel connected to.
            /// </summary>
            public string? Channel { get; set; }

            /// <summary>
            ///     Gets or sets the channel id from twitch.
            /// </summary>
            public string? ChannelId { get; set; }

            /// <summary>
            ///     Gets or sets the callbacks to handle chat messages.
            /// </summary>
            public Action<TwitchClient, OnMessageReceivedArgs>? MessageCallbacks { get; set; }
        }

        #endregion
    }
}