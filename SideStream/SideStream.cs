using Dalamud.Plugin;
using SideStream.Attributes;
using SideStream.Common;
using System;

namespace SideStream
{
    public class SideStream : IDalamudPlugin
    {
        private DalamudPluginInterface pluginInterface;
        private PluginCommandManager<SideStream> commandManager;
        private Configuration config;
        private PluginUI ui;

        private TwitchChatClient twitch;

        public string Name => "SideStream";

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;

            this.config = (Configuration)this.pluginInterface.GetPluginConfig() ?? new Configuration();
            this.config.Initialize(this.pluginInterface);

            this.twitch = new TwitchChatClient("karashiir", Environment.GetEnvironmentVariable("TWITCH_OAUTH_TOKEN"));

            this.ui = new PluginUI();
            this.pluginInterface.UiBuilder.OnBuildUi += this.ui.Draw;
            this.pluginInterface.UiBuilder.OnOpenConfigUi = (s, a) => this.ui.IsVisible = true;

            this.commandManager = new PluginCommandManager<SideStream>(this, this.pluginInterface);
        }

        [Command("/sidestream")]
        [HelpMessage("Toggles the SideStream window.")]
        public void ToggleUI(string command, string args)
        {
            this.ui.IsVisible = !this.ui.IsVisible;
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            this.commandManager.Dispose();

            this.pluginInterface.SavePluginConfig(this.config);

            this.pluginInterface.UiBuilder.OnBuildUi -= this.ui.Draw;

            this.pluginInterface.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
