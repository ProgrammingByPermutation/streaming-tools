namespace streaming_tools {
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using JetBrains.Annotations;
    using Newtonsoft.Json;

    /// <summary>
    ///     The persisted user configuration.
    /// </summary>
    public class Configuration : INotifyPropertyChanged {
        /// <summary>
        ///     The location the file should be saved and read from.
        /// </summary>
        private static readonly string CONFIG_FILENAME = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData") ?? string.Empty, "nullinside", "streaming-tools", "config.json");

        /// <summary>
        ///     The singleton instance of the class.
        /// </summary>
        private static Configuration? instance;

        /// <summary>
        ///     The command to execute on keystroke.
        /// </summary>
        private KeystokeCommand? keystokeCommand;

        /// <summary>
        ///     The monitor selected in the layout view.
        /// </summary>
        private string? layoutSelectedMonitor;

        /// <summary>
        ///     The <seealso cref="Guid" /> of the microphone used to pause TTS.
        /// </summary>
        private string? microphoneGuid;

        /// <summary>
        ///     The 0% - 100% microphone volume at which to pause TTS.
        /// </summary>
        private int pauseThreshold;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Configuration" /> class.
        /// </summary>
        /// <remarks>Prevents the class from being instantiated outside of our singleton.</remarks>
        protected Configuration() { }

        /// <summary>
        ///     Gets the singleton instance of our class.
        /// </summary>
        public static Configuration Instance {
            get {
                if (null == Configuration.instance) {
                    Configuration.instance = Configuration.ReadConfiguration();
                }

                return Configuration.instance;
            }
        }

        /// <summary>
        ///     Gets or sets the audio channel that sound channel point redemptions use.
        /// </summary>
        public string? ChannelPointSoundRedemptionOutputChannel { get; set; }

        /// <summary>
        ///     Gets or sets the channel point redemption sounds.
        /// </summary>
        public ObservableCollection<ChannelPointSoundRedemption>? ChannelPointSoundRedemptions { get; set; }

        /// <summary>
        ///     Gets or sets the master volume to scale all channel point redemption sounds at.
        /// </summary>
        public uint? ChannelPointSoundRedemptionsMasterVolume { get; set; }

        /// <summary>
        ///     Gets or sets the command to execute on keystroke.
        /// </summary>
        public KeystokeCommand? KeystokeCommand {
            get => this.keystokeCommand;
            set {
                this.keystokeCommand = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the monitor selected in the layout view.
        /// </summary>
        public string? LayoutSelectedMonitor {
            get => this.layoutSelectedMonitor;
            set {
                this.layoutSelectedMonitor = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the <seealso cref="Guid" /> of the microphone used to pause TTS.
        /// </summary>
        public string? MicrophoneGuid {
            get => this.microphoneGuid;
            set {
                if (value == this.microphoneGuid) {
                    return;
                }

                this.microphoneGuid = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the percentage of microphone audio at which point text to speech pauses.
        /// </summary>
        public int PauseThreshold {
            get => this.pauseThreshold;
            set {
                if (value == this.pauseThreshold) {
                    return;
                }

                this.pauseThreshold = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the collection of usernames that the bot doesn't pronounce correctly.
        /// </summary>
        public ObservableCollection<KeyValuePair<string, string>>? TtsPhoneticUsernames { get; set; }

        /// <summary>
        ///     Gets or sets the collection of usernames to not read messages from in twitch chat.
        /// </summary>
        public ObservableCollection<string>? TtsUsernamesToSkip { get; set; }

        /// <summary>
        ///     Gets or sets the collection of all twitch accounts.
        /// </summary>
        public ObservableCollection<TwitchAccount>? TwitchAccounts { get; set; }

        /// <summary>
        ///     Gets or sets the collection of twitch chat configurations.
        /// </summary>
        public ObservableCollection<TwitchChatConfiguration>? TwitchChatConfigs { get; set; }

        /// <summary>
        ///     Raised when a property is changed on this object.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        ///     Read the configuration from disk.
        /// </summary>
        /// <returns>The configuration object.</returns>
        public static Configuration ReadConfiguration() {
            Configuration? config = null;

            try {
                if (File.Exists(Configuration.CONFIG_FILENAME)) {
                    JsonSerializer serializer = new();
                    using (StreamReader sr = new StreamReader(Configuration.CONFIG_FILENAME))
                    using (JsonReader jr = new JsonTextReader(sr)) {
                        config = serializer.Deserialize<Configuration>(jr);
                    }
                }
            } catch (Exception) { }

            if (null == config) {
                config = new Configuration();
            }

            if (null == config.TwitchAccounts) {
                config.TwitchAccounts = new ObservableCollection<TwitchAccount>();
            }

            if (null == config.TwitchChatConfigs) {
                config.TwitchChatConfigs = new ObservableCollection<TwitchChatConfiguration>();
            }

            if (null == config.TtsUsernamesToSkip) {
                config.TtsUsernamesToSkip = new ObservableCollection<string>();
            }

            if (null == config.TtsPhoneticUsernames) {
                config.TtsPhoneticUsernames = new ObservableCollection<KeyValuePair<string, string>>();
            }

            if (null == config.KeystokeCommand) {
                config.KeystokeCommand = new KeystokeCommand();
            }

            if (null == config.ChannelPointSoundRedemptions) {
                config.ChannelPointSoundRedemptions = new ObservableCollection<ChannelPointSoundRedemption>();
            }

            if (null == config.ChannelPointSoundRedemptionsMasterVolume) {
                config.ChannelPointSoundRedemptionsMasterVolume = 100;
            }

            return config;
        }

        /// <summary>
        ///     Gets the twitch account object associated with the username.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns>The <see cref="TwitchAccount" /> object if found, null otherwise.</returns>
        public TwitchAccount? GetTwitchAccount(string? username) {
            if (null == this.TwitchAccounts || string.IsNullOrWhiteSpace(username)) {
                return null;
            }

            return this.TwitchAccounts.FirstOrDefault(a => null != a.Username && a.Username.Equals(username, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        ///     Write the configuration to disk.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool WriteConfiguration() {
            try {
                var dirName = Path.GetDirectoryName(Configuration.CONFIG_FILENAME);
                if (null != dirName && !Directory.Exists(dirName)) {
                    Directory.CreateDirectory(dirName);
                }

                JsonSerializer serializer = new();
                using (StreamWriter sr = new StreamWriter(Configuration.CONFIG_FILENAME))
                using (JsonWriter jr = new JsonTextWriter(sr)) {
                    serializer.Serialize(jr, this);
                }
            } catch (Exception) {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Raised when a property changes on this class.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    ///     Represents a twitch account.
    /// </summary>
    public class TwitchAccount {
        /// <summary>
        ///     Gets or sets the twitch api OAuth token.
        /// </summary>
        public string? ApiOAuth { get; set; }

        /// <summary>
        ///     Gets or sets the date time at which the OAuth token expires.
        /// </summary>
        public DateTime? ApiOAuthExpires { get; set; }

        /// <summary>
        ///     Gets or sets the refresh token used to refresh the <see cref="ApiOAuth" />.
        /// </summary>
        public string? ApiOAuthRefresh { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the account is the account the user uses to stream.
        /// </summary>
        public bool IsUsersStreamingAccount { get; set; }

        /// <summary>
        ///     Gets or sets the twitch username.
        /// </summary>
        public string? Username { get; set; }
    }

    /// <summary>
    ///     Represents a single connection to a twitch chat by a single user.
    /// </summary>
    public class TwitchChatConfiguration {
        /// <summary>
        ///     Gets or sets the twitch username.
        /// </summary>
        public string? AccountUsername { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the bot should provide administration commands.
        /// </summary>
        public bool AdminOn { get; set; }

        /// <summary>
        ///     Gets or sets the output device to send audio to.
        /// </summary>
        public string? OutputDevice { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether text to speech should pause when someone talks into the microphone.
        /// </summary>
        public bool PauseDuringSpeech { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether text to speech is on.
        /// </summary>
        public bool TtsOn { get; set; }

        /// <summary>
        ///     Gets or sets the selected Microsoft Text to Speech voice.
        /// </summary>
        public string? TtsVoice { get; set; }

        /// <summary>
        ///     Gets or sets the volume of the text to speech voice.
        /// </summary>
        public uint TtsVolume { get; set; }

        /// <summary>
        ///     Gets or sets the twitch channel to read chat from.
        /// </summary>
        public string? TwitchChannel { get; set; }
    }

    /// <summary>
    ///     Represents a command to execute on keystroke.
    /// </summary>
    public class KeystokeCommand {
        /// <summary>
        ///     Gets or sets the command to type in chat.
        /// </summary>
        public string? Command { get; set; }

        /// <summary>
        ///     Gets or sets the key code of the keystroke.
        /// </summary>
        public int? KeyCode { get; set; }

        /// <summary>
        ///     Gets or sets the twitch chat to type in.
        /// </summary>
        public string? TwitchChat { get; set; }
    }

    /// <summary>
    ///     The mapping of a channel point redemption to its sound.
    /// </summary>
    public class ChannelPointSoundRedemption {
        /// <summary>
        ///     Gets or sets the filename of the sound to play.
        /// </summary>
        public string? Filename { get; set; }

        /// <summary>
        ///     Gets or sets the name of the channel point redemption associated with the sound.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        ///     Gets or sets the volume of the channel point sound.
        /// </summary>
        public int? Volume { get; set; }
    }
}