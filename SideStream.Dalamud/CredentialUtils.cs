using System.Net;
using AdysTech.CredentialManager;

namespace SideStream.Dalamud
{
    public static class CredentialUtils
    {
        private const string Target = "SideStream_OAuth2_Twitch";

        public static NetworkCredential GetPluginCredential()
            => CredentialManager.GetCredentials(Target);

        public static void SavePluginCredential(string username, string oauthToken)
        {
            var credential = new NetworkCredential(username, oauthToken);
            CredentialManager.SaveCredentials(Target, credential);
        }

        public static void RemovePluginCredential()
            => CredentialManager.RemoveCredentials(Target);
    }
}