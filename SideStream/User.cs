using System;

namespace SideStream
{
    public class User
    {
        public string Name { get; }
        public uint Color { get; }

        public User(string username)
        {
            Name = username;

            var colors = Enum.GetNames(typeof(TwitchUsernameColors));
            var index = new Random().Next(colors.Length);
            Color = (uint)Enum.Parse(typeof(TwitchUsernameColors), colors[index]);
        }
    }
}