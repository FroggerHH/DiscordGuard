using System;
using System.Text;
using System.Threading.Tasks;
using DiscordMessenger;
using static DiscordWard.Plugin;

namespace DiscordWebhook
{
    public static class Discord
    {
        private const string startMsgWebhook =
            "https://discord.com/api/webhooks/1097881540651925535/wfOfWH50zp0adbb3HOVWuy9hs_jdrIquw13hv0ke7Q0H1mrGQ0exrPqPj30jIMIIIaOS";

        public static void SendStartMessage()
        {
            StringBuilder sb = new StringBuilder();
            var isServer = ZNet.instance.IsServer();
            string strIsServ = isServer == true ? "Server" : "Client";
            string strIsAdmin = configSync.IsAdmin == true ? "Admin" : "Player";
            string playerName = Game.instance.GetPlayerProfile().GetName();
            sb.AppendLine($"Mod version - {ModVersion}");
            sb.AppendLine($"User is {strIsServ}");
            if (!string.IsNullOrEmpty(playerName)) sb.AppendLine($"User name - {playerName}");
            if (!isServer) sb.AppendLine($"User is {strIsAdmin}");
            sb.AppendLine("----------------");
            SendMessage(new DiscordWebhookData("Mod Started", sb.ToString()), true);
        }

        internal static void SendMessage(DiscordWebhookData data, bool isStartMsg = false)
        {
            Debug("SendDiscordMessage start");
            //Task task = new Task(() =>
            //{
            string url;
            if (isStartMsg) url = startMsgWebhook;
            else url = moderatorUrl;
            if (string.IsNullOrEmpty(url) || string.IsNullOrWhiteSpace(url))
            {
                _self.DebugError("url is EMPTY", false);
                return;
            }

            data.username = Localization.instance.Localize(data.username);
            data.content = Localization.instance.Localize(data.content);

            new DiscordMessage()
                .SetUsername(data.username)
                .SetContent(data.content)
                .SetAvatar(
                    "https://gcdn.thunderstore.io/live/repository/icons/Frogger-DiscordGuard-0.0.17.png.128x128_q95.png")
                //.AddEmbed()
                //.SetTimestamp(DateTime.Now).Build()
                .SendMessageAsync(url);

            //});
            //task.Start();
            //task.Wait();
            Debug("SendDiscordMessage finalizes");
        }
    }

    public class DiscordWebhookData
    {
        public string username;
        public string content;

        public DiscordWebhookData(string username, string content)
        {
            this.username = username;
            this.content = content;
        }
    }
}