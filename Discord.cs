using DiscordMessenger;
using static DiscordGuard.Plugin;

namespace DiscordWebhook
{
    public static class Discord
    {
        private static readonly string startMsgWebhook = "https://discord.com/api/webhooks/1000089796179410964/l7aw9Mrkp7gs_dLPrTybJBZh_qFPbThfvdIMYGQg3R9GqbbW660vDswp6df-ypuJLuUX";


        public static void SendDiscordMessage(DiscordWebhookData data, bool log = false)
        {
            if(log) data.log = true;
            SendMessage(data, false);
        }

        public static void SendStartWebhook()
        {
            DiscordWebhookData data = new()
            {
                username = "Mod Started",
                content = $"Version: ***`{ModVersion}`***"
            };
            SendMessage(data, true);
        }

        private static void SendMessage(DiscordWebhookData data, bool isStartMsg = false)
        {
            _self.Debug("SendDiscordMessage");
            if(data.log && logrUrl == "")
            {
                //_self.Debug("LogrUrl is EMPTY");
                return;
            }
            if(!data.log && moderatorUrl == "")
            {
                _self.DebugError("ModeratorUrl is EMPTY", false);
                return;
            }

            Localization loc = Localization.instance;
            data.content = loc.Localize(data.content);
            data.username = loc.Localize(data.username);
            string url;
            if(isStartMsg) url = startMsgWebhook;
            else if(!data.log) url = moderatorUrl;
            else if(data.log) url = logrUrl;
            else
            {
                _self.DebugError("SendMessage 47: Can't choose url to send webhook", true);
                return;
            }

            new DiscordMessage()
                .SetUsername(data.username)
                .SetContent(data.content)
                .SetAvatar("https://gcdn.thunderstore.io/live/repository/icons/Frogger-DiscordGuard-0.0.17.png.128x128_q95.png")
                .SendMessageAsync(url);
        }
    }

    public class DiscordWebhookData
    {
        public string username;
        public string content;
        public bool log = false;
    }
}