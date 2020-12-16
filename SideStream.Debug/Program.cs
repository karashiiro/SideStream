using System;
using System.Threading;

namespace SideStream.Debug
{
    public static class Program
    {
        public static void Main()
        {
            var thread = new Thread(() =>
            {
                var twitch = new TwitchChatClient("karashiir", Environment.GetEnvironmentVariable("TWITCH_OAUTH_TOKEN"));
                twitch.OnChatConnected += () =>
                {
                    Console.WriteLine("Connected.");
                    twitch.ConnectChannel("karashiir");
                };
                twitch.OnLocalUserJoinedChannel += (channel, text) =>
                {
                    Console.WriteLine("Joined channel #{0}", channel);
                };
                twitch.OnChannelMessageReceived += (message) =>
                {
                    Console.WriteLine("{0}: {1} {2}", message.Sender, message.CompoundScore, message.Text);
                };
                Console.WriteLine("Started.");
            });
            thread.Start();
            Thread.Sleep(-1);
        }
    }
}
