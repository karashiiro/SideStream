using System;
using ImGuiNET;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using ImGuiWindowFlags = ImGuiNET.ImGuiWindowFlags;

namespace SideStream.Dalamud
{
    public class PluginUI
    {
        private static readonly Vector4 Blue = ImGui.ColorConvertU32ToFloat4(0xFFFFB300);
        private static readonly Vector4 DarkBlue = ImGui.ColorConvertU32ToFloat4(0xFF694900);
        private static readonly Vector4 LightBlue = ImGui.ColorConvertU32ToFloat4(0xFFFFDD00);
        private static readonly Vector4 HintColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);

        private readonly Configuration config;
        private readonly Action<TwitchChatClient> loginCallback;
        private TwitchChatClient twitch;
        private readonly IDictionary<string, ConcurrentQueue<ChatMessage>> chatState;
        private string currentChannel;
        private bool hideNegativeMessages;
        private Vector2 windowSize;

        private bool isVisible;
        public bool IsVisible { get => isVisible; set => isVisible = value; }

        public PluginUI(TwitchChatClient twitch, Action<TwitchChatClient> loginCallback, Configuration config)
        {
            this.config = config;
            this.loginCallback = loginCallback;
            this.twitch = twitch;
            this.chatState = new ConcurrentDictionary<string, ConcurrentQueue<ChatMessage>>();
            this.currentChannel = null;

            if (this.twitch != null)
                LoadChannels();

            this.windowSize = new Vector2(600, 400);

#if DEBUG
            this.isVisible = true;
#endif
        }

        private bool configLoaded;
        private void LoadChannels()
        {
            foreach (var channel in this.config.Channels)
                RegisterChannel(channel);
            this.configLoaded = true;
        }

        private void RegisterChannel(string channel)
        {
            if (this.chatState.ContainsKey(channel))
            {
                this.currentChannel = channel;
                return;
            }
            
            if (!this.config.Channels.Contains(channel))
            {
                this.config.Channels.Add(channel);
                this.config.Save();
            }

            this.chatState.Add(channel, new ConcurrentQueue<ChatMessage>());
            this.currentChannel = channel;
            this.twitch.ConnectChannel(channel);
            this.twitch.OnChannelMessageReceived += PushMessageCurried(channel);
        }

        private void UnregisterChannel(string channel)
        {
            this.config.Channels.Remove(channel);
            this.config.Save();
            this.chatState.Remove(channel);
            this.twitch.DisconnectChannel(channel);
            this.twitch.OnChannelMessageReceived -= PushMessageCurried(channel);
        }

        private TwitchChannelMessageReceived PushMessageCurried(string channel)
        {
            return message =>
            {
                PushMessage(channel, message);
            };
        }

        private void PushMessage(string channel, ChatMessage message)
        {
            this.chatState[channel].Enqueue(message);
            while (this.chatState[channel].Count > 5000)
                this.chatState[channel].TryDequeue(out _);
        }

        private string chatInput = string.Empty;
        public void Draw()
        {
            if (!IsVisible)
                return;

            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, Blue);
            ImGui.PushStyleColor(ImGuiCol.CheckMark, Blue);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, LightBlue);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, DarkBlue);
            if (this.twitch != null)
            {
                ImGui.SetNextWindowSize(this.windowSize, ImGuiCond.FirstUseEver);
                ImGui.Begin("SideStream", ref this.isVisible, ImGuiWindowFlags.None);
                this.windowSize = ImGui.GetWindowSize();
                DrawMain();
            }
            else
            {
                ImGui.Begin("SideStream", ref this.isVisible, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize);
                DrawLogin();
            }
            ImGui.End();
            ImGui.PopStyleColor(4);
        }

        private string username = string.Empty;
        private string oauthToken = string.Empty;
        private void DrawLogin()
        {
            ImGui.InputTextWithHint("##SideStreamAuthUsername", "Username", ref this.username, 15);
            ImGui.InputTextWithHint("##SideStreamAuthOAuth2", "OAuth2 Token", ref this.oauthToken, 50);

            if (ImGui.Button("Login##SideStreamAuthLogin"))
            {
                var loggedInTwitch = new TwitchChatClient(this.username, this.oauthToken);

                CredentialUtils.SavePluginCredential(this.username, this.oauthToken);
                this.twitch = loggedInTwitch;
                this.loginCallback(loggedInTwitch);

                if (!this.configLoaded)
                    LoadChannels();

                ImGui.SetWindowSize(this.windowSize);

                this.username = string.Empty;
                this.oauthToken = string.Empty;
            }

            ImGui.TextColored(HintColor, "Credentials secured with Windows Credential Manager");

            ImGui.Spacing();
            if (ImGui.Button("Get OAuth2 Token##SideStreamAuthGetToken"))
            {
                Process.Start("https://twitchapps.com/tmi/");
            }
        }

        private void DrawMain()
        {
            DrawTabs();

            ImGui.BeginChildFrame(0x92929292, ImGui.GetWindowSize() - new Vector2(16f, 94f));
            if (!string.IsNullOrEmpty(this.currentChannel))
            {
                foreach (var message in this.chatState[this.currentChannel])
                {
                    if (this.hideNegativeMessages && message.CompoundScore < 0)
                        continue;

                    if (this.config.Bad.Any(t => t.Text != "" && t.Match(message.Text)))
                        continue;

                    ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(message.Sender.Color), message.Sender.Name);
                    ImGui.SameLine(ImGui.CalcTextSize(message.Sender.Name).X + 6);
                    ImGui.TextWrapped($": {message.Text}");
                }
            }
            ImGui.EndChildFrame();

            if (ImGui.InputTextWithHint(
                "##SideStreamChatInput",
                "Send a message",
                ref this.chatInput,
                500,
                ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if (!string.IsNullOrEmpty(this.currentChannel))
                {
                    this.twitch.SendMessage(this.currentChannel, this.chatInput);
                    PushMessage(this.currentChannel, new ChatMessage
                    {
                        Sender = new User(this.twitch.Username),
                        Text = this.chatInput,
                        // Analysis left at 0 for everything so your own messages always show up
                    });
                    this.chatInput = string.Empty;
                }
            }

            ImGui.SameLine();
            ImGui.Checkbox("Hide negative messages", ref this.hideNegativeMessages);
        }

        private string nextChannelName = string.Empty;
        private void DrawTabs()
        {
            ImGui.BeginTabBar("SideStream##Tabs");
            {
                foreach (var channel in this.chatState.Keys)
                {
                    var open = channel == this.currentChannel;
                    var openAfter = open;
                    if (!ImGui.BeginTabItem($"{channel}##SideStream", ref openAfter)) continue;
                    if (open != openAfter)
                    {
                        UnregisterChannel(this.currentChannel);
                        this.currentChannel = this.chatState.Keys.FirstOrDefault();
                        ImGui.EndTabItem();
                        continue;
                    }
                    SwitchChannel(channel);
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("+##SideStreamAddChannel"))
                {
                    if (ImGui.BeginPopupContextItem("##SideStreamAddChannelInputPopup", 0))
                    {
                        if (ImGui.InputTextWithHint(
                            "##SideStreamAddChannelInput",
                            "Enter channel name",
                            ref this.nextChannelName,
                            15,
                            ImGuiInputTextFlags.EnterReturnsTrue))
                        {
                            RegisterChannel(this.nextChannelName);
                            SwitchChannel(this.nextChannelName);
                            this.nextChannelName = string.Empty;
                            ImGui.CloseCurrentPopup();
                        }
                        ImGui.EndPopup();
                    }
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Settings##SideStreamSettings"))
                {
                    if (ImGui.BeginPopupContextItem("##SideStreamSettingsPopup", 0))
                    {
                        if (ImGui.Selectable("Logout"))
                        {
                            this.twitch = null;
                            SwitchChannel(null);
                            CredentialUtils.RemovePluginCredential();
                            ImGui.CloseCurrentPopup();
                        }
                        ImGui.EndPopup();
                    }
                    ImGui.EndTabItem();
                }
            }
            ImGui.EndTabBar();
        }

        private void SwitchChannel(string channel)
        {
            this.currentChannel = channel;
            this.chatInput = string.Empty;
        }
    }
}