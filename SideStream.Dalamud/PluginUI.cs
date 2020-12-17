using ImGuiNET;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SideStream.Dalamud
{
    public class PluginUI
    {
        private static readonly Vector4 Blue = ImGui.ColorConvertU32ToFloat4(0xFFFFB300);
        private static readonly Vector4 DarkBlue = ImGui.ColorConvertU32ToFloat4(0xFF694900);
        private static readonly Vector4 LightBlue = ImGui.ColorConvertU32ToFloat4(0xFFFFDD00);

        private readonly TwitchChatClient twitch;
        private readonly IDictionary<string, ConcurrentQueue<ChatMessage>> chatState;
        private string currentChannel;
        private bool hideNegativeMessages;

        private bool isVisible;
        public bool IsVisible { get => isVisible; set => isVisible = value; }

        public PluginUI(TwitchChatClient twitch)
        {
            this.twitch = twitch;
            this.chatState = new ConcurrentDictionary<string, ConcurrentQueue<ChatMessage>>();
            this.currentChannel = null;
        }

        private void RegisterChannel(string channel)
        {
            this.chatState.Add(channel, new ConcurrentQueue<ChatMessage>());
            this.currentChannel = channel;
            this.twitch.ConnectChannel(this.nextChannelName);
            this.twitch.OnChannelMessageReceived += message =>
            {
                PushMessage(this.nextChannelName, message);
            };
        }

        private void UnregisterChannel(string channel)
        {
            this.chatState.Remove(channel);
            this.twitch.DisconnectChannel(channel);
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
            ImGui.Begin("SideStream", ref this.isVisible, ImGuiWindowFlags.None);
            {
                DrawTabs();
                
                ImGui.BeginChildFrame(0x92929292, ImGui.GetWindowSize() - new Vector2(16f, 94f));
                if (!string.IsNullOrEmpty(this.currentChannel))
                {
                    foreach (var message in this.chatState[this.currentChannel])
                    {
                        if (this.hideNegativeMessages && message.CompoundScore < 0)
                            continue;
                        ImGui.TextWrapped($"{message.Sender}: {message.Text}");
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
                            Sender = this.twitch.Username,
                            Text = this.chatInput,
                            // Analysis left at 0 for everything so your own messages always show up
                        });
                        this.chatInput = string.Empty;
                    }
                }

                ImGui.SameLine();
                ImGui.Checkbox("Hide negative messages", ref this.hideNegativeMessages);
            }
            ImGui.End();
            ImGui.PopStyleColor(4);
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

                if (ImGui.BeginTabItem("+##SideStream"))
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