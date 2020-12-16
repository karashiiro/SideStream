using System;
using System.Linq;
using IrcDotNet;

namespace SideStream
{
    public delegate void TwitchChatConnected();
    public delegate void TwitchChatConnectFailed();
    public delegate void TwitchChatDisconnected();
    public delegate void TwitchChatErrored();
    public delegate void TwitchChatRegistered();

    public delegate void TwitchLocalUserNoticeReceived(string sender, string text);
    public delegate void TwitchLocalUserMessageReceived(ChatMessage message);
    public delegate void TwitchLocalUserJoinedChannel(string channel, string text);
    public delegate void TwitchLocalUserLeftChannel(string channel, string text);

    public delegate void TwitchChannelUserJoined(string sender, string text);
    public delegate void TwitchChannelUserLeft(string sender, string text);
    public delegate void TwitchChannelMessageReceived(ChatMessage message);
    public delegate void TwitchChannelNoticeReceived(string sender, string text);

    public class TwitchChatClient : IDisposable
    {
        private readonly TwitchIrcClient _client;

        public TwitchChatConnected OnChatConnected { get; set; }
        public TwitchChatConnectFailed OnChatConnectFailed { get; set; }
        public TwitchChatDisconnected OnChatDisconnected { get; set; }
        public TwitchChatErrored OnChatErrored { get; set; }
        public TwitchChatRegistered OnChatRegistered { get; set; }

        public TwitchLocalUserNoticeReceived OnLocalUserNoticeReceived { get; set; }
        public TwitchLocalUserMessageReceived OnLocalUserMessageReceived { get; set; }
        public TwitchLocalUserJoinedChannel OnLocalUserJoinedChannel { get; set; }
        public TwitchLocalUserLeftChannel OnLocalUserLeftChannel { get; set; }

        public TwitchChannelUserJoined OnChannelUserJoined { get; set; }
        public TwitchChannelUserLeft OnChannelUserLeft { get; set; }
        public TwitchChannelMessageReceived OnChannelMessageReceived { get; set; }
        public TwitchChannelNoticeReceived OnChannelNoticeReceived { get; set; }

        public string Username { get; }

        public TwitchChatClient(string username, string oauthToken)
        {
            Username = username;

            _client = new TwitchIrcClient {FloodPreventer = new IrcStandardFloodPreventer(4, 2000)};
            _client.Registered += Registered;
            _client.Disconnected += Disconnected;
            _client.Connected += Connected;
            _client.ConnectFailed += ConnectFailed;
            _client.Error += Errored;

            _client.Connect("irc.twitch.tv", false, new IrcUserRegistrationInfo
            {
                NickName = username,
                Password = oauthToken,
                UserName = username,
            });
        }

        public void ConnectChannel(string channel) => _client.Channels.Join($"#{channel}");

        public void DisconnectChannel(string channel) => _client.Channels.Leave($"#{channel}");

        public void SendMessage(string channel, string message) => _client.SendRawMessage($"PRIVMSG #{channel} {message}");

        public void Disconnect() => _client.Disconnect();

        private void Connected(object sender, EventArgs e) => OnChatConnected?.Invoke();
        private void ConnectFailed(object sender, EventArgs e) => OnChatConnectFailed?.Invoke();
        private void Errored(object sender, EventArgs e) => OnChatErrored?.Invoke();

        private void Registered(object sender, EventArgs e)
        {
            _client.LocalUser.NoticeReceived += LocalUserNoticeReceived;
            _client.LocalUser.MessageReceived += LocalUserMessageReceived;
            _client.LocalUser.JoinedChannel += LocalUserJoinedChannel;
            _client.LocalUser.LeftChannel += LocalUserLeftChannel;

            OnChatRegistered?.Invoke();
        }

        private void LocalUserJoinedChannel(object sender, IrcChannelEventArgs e)
        {
            e.Channel.UserJoined += ChannelUserJoined;
            e.Channel.UserLeft += ChannelUserLeft;
            e.Channel.MessageReceived += ChannelMessageReceived;
            e.Channel.NoticeReceived += ChannelNoticeReceived;

            OnLocalUserJoinedChannel?.Invoke(e.Channel.Name.Substring(1), e.Comment);
        }

        private void ChannelNoticeReceived(object sender, IrcMessageEventArgs e)
            => OnChannelNoticeReceived?.Invoke(e.Source.Name, e.Text);

        private void ChannelMessageReceived(object sender, IrcMessageEventArgs e)
        {
            var analysis = ChatProcessingUtils.AnalyzeSentiment(e.Text);
            OnChannelMessageReceived?.Invoke(new ChatMessage
            {
                Text = e.Text,
                Sender = e.Source.Name,
                NegativeScore = analysis.Negative,
                CompoundScore = analysis.Compound,
                NeutralScore = analysis.Neutral,
                PositiveScore = analysis.Positive,
            });
        }

        private void ChannelUserLeft(object sender, IrcChannelUserEventArgs e)
            => OnChannelUserLeft?.Invoke(e.ChannelUser.User.UserName, e.Comment);

        private void ChannelUserJoined(object sender, IrcChannelUserEventArgs e)
            => OnChannelUserJoined?.Invoke(e.ChannelUser.User.UserName, e.Comment);

        private void LocalUserLeftChannel(object sender, IrcChannelEventArgs e)
        {
            e.Channel.UserJoined -= ChannelUserJoined;
            e.Channel.UserLeft -= ChannelUserLeft;
            e.Channel.MessageReceived -= ChannelMessageReceived;
            e.Channel.NoticeReceived -= ChannelNoticeReceived;

            OnLocalUserLeftChannel?.Invoke(e.Channel.Name.Substring(1), e.Comment);
        }

        private void LocalUserMessageReceived(object sender, IrcMessageEventArgs e)
        {
            var analysis = ChatProcessingUtils.AnalyzeSentiment(e.Text);
            OnLocalUserMessageReceived?.Invoke(new ChatMessage
            {
                Text = e.Text,
                Sender = e.Source.Name,
                NegativeScore = analysis.Negative,
                CompoundScore = analysis.Compound,
                NeutralScore = analysis.Neutral,
                PositiveScore = analysis.Positive,
            });
        }

        private void LocalUserNoticeReceived(object sender, IrcMessageEventArgs e)
            => OnLocalUserNoticeReceived?.Invoke(e.Source.Name, e.Text);

        private void Disconnected(object sender, EventArgs e)
            => OnChatDisconnected?.Invoke();

        public void Dispose() => _client.Dispose();
    }
}