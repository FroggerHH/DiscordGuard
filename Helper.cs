namespace DiscordGuard;

public static class Helper
{
    public static bool GetAreaOwnerName(PrivateArea area, out string ownerName)
    {
        ownerName = "-none-";
        if (!area || !area.m_nview || !Player.m_localPlayer) return false;
        var zdo = area.m_nview.GetZDO();
        if (zdo == null) return false;

        ownerName = zdo.GetString("creatorName");
        if(string.IsNullOrEmpty(ownerName) || string.IsNullOrWhiteSpace(ownerName)) return false;
        return true;
    }
    public static bool GetCurrentAreaOwnerName(out string ownerName)
    {
        PrivateArea area = Plugin.current;
        ownerName = "-none-";
        if (!area || !area.m_nview || !Player.m_localPlayer) return false;
        var zdo = area.m_nview.GetZDO();
        if (zdo == null) return false;

        ownerName = zdo.GetString("creatorName");
        if(string.IsNullOrEmpty(ownerName) || string.IsNullOrWhiteSpace(ownerName)) return false;
        return true;
    }
    public static string GetPlayerName()
    {
        if (Player.m_localPlayer) return Player.m_localPlayer.GetPlayerName();
        return "-none-";
    }
}