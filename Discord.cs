using System;
using System.Text;
using System.Threading.Tasks;
using DiscordMessenger;
using static DiscordGuard.Plugin;

namespace DiscordWebhook
{
    public static class Discord
    {
        private const string startMsgWebhook =
            "https://discord.com/api/webhooks/1000089796179410964/l7aw9Mrkp7gs_dLPrTybJBZh_qFPbThfvdIMYGQg3R9GqbbW660vDswp6df-ypuJLuUX";

        public static void SendStartMessage()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"version - {ModVersion}");
            var isServer = ZNet.instance.IsServer();
            sb.AppendLine(isServer == true ? "Server" : "Client");
            if (!isServer) sb.AppendLine($"user - {Game.instance.GetPlayerProfile()?.GetName()}");
            SendMessage(new DiscordWebhookData("Mod Started", sb.ToString()), true);
        }

        internal static void SendMessage(DiscordWebhookData data, bool isStartMsg = false)
        {
            Task task = new Task(() =>
            {
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
                    .SetTime(DateTime.Now)
                    .SendMessageAsync(url);
            });
            task.Start();
            task.Wait();
            Debug("SendDiscordMessage");
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