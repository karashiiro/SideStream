﻿using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;

namespace SideStream.Dalamud
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; }

        public IList<string> Channels { get; set; }

        public IList<Trigger> Bad { get; set; }

        // Add any other properties or methods here.
        [JsonIgnore] private DalamudPluginInterface pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;

            Channels ??= new List<string>();

            Bad ??= new List<Trigger>();
        }

        public void Save()
        {
            this.pluginInterface.SavePluginConfig(this);
        }
    }
}
