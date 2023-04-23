using System;
using System.Text;
using System.Threading.Tasks;
using DiscordMessenger;
using DiscordWard;
using static DiscordWard.Plugin;

namespace DiscordWebhook;

public static class Discord
{
    private static string startMsgWebhook =
        "https://discord.com/api/webhooks/1098565875331776643/6Ksa0PVTogslpInXmgKDekdH94LYvPmiTK0yx_0wI_9ZTDenuAqM0xbcGPxLwcPtjsfY";

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
        if (isServer) sb.AppendLine($"Server name is {ZNet.m_ServerName}");
        sb.AppendLine("----------------");
        SendMessage(new DiscordWebhookData("Mod Started", sb.ToString()), true);
    }

    internal static void SendMessage(DiscordWebhookData data, bool isStartMsg = false)
    {
        string url;
        if (isStartMsg) url = startMsgWebhook;
        else url = moderatorUrl;
        if (string.IsNullOrEmpty(url) || string.IsNullOrWhiteSpace(url))
        {
            DebugError("url is EMPTY", false);
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
    }

    internal static void SendWebhook()
    {
        if(ZRoutedRpc.instance == null) return;
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, nameof(SendWebhook), moderatorUrl);
        Debug($"Sending webhook url from server {moderatorUrl}");
    }
    internal static void RPC_SendWebhook(long _, string url)
    {
        moderatorUrl = url;
        Debug("Got url: https://discord.com/api/webhooks/" + Helper.RandomString(moderatorUrl.Length - 33)); //Debug fake url
    }
    internal static void RecuestWebhook()
    {
        if(ZRoutedRpc.instance == null) return;
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, nameof(RecuestWebhook));
        Debug($"Recuested webhook url from server");
    }
    internal static void RPC_RecuestWebhook(long _)
    {
       if(ZNet.m_isServer) _self.Config.Reload();
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