﻿using DiscordWebhook;
using static DiscordWard.Plugin;

namespace DiscordWard;

public static class Helper
{
    public static bool GetAreaOwnerName(PrivateArea area, out string ownerName)
    {
        ownerName = "-none-";
        if (!area || !area.m_nview || !Player.m_localPlayer) return false;
        var zdo = area.m_nview.GetZDO();
        if (zdo == null) return false;

        ownerName = zdo.GetString("creatorName");
        if (string.IsNullOrEmpty(ownerName) || string.IsNullOrWhiteSpace(ownerName)) return false;
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
        if (string.IsNullOrEmpty(ownerName) || string.IsNullOrWhiteSpace(ownerName)) return false;
        return true;
    }

    public static string GetPlayerName()
    {
        if (Player.m_localPlayer) return Player.m_localPlayer.GetPlayerName();
        return "-none-";
    }

    public static bool CheckAccess(out bool isOwner)
    {
        isOwner = current.m_piece.IsCreator();
        return PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, false, true);
    }

    public static void SimplePatch(string sendKey, bool hold)
    {
        if (hold) return;
        if (PatchCheck(ref sendKey, out var creatorName, out var playerName)) return;
        DiscordWebhookData data = new($"$Guard {creatorName}", $"{playerName} {sendKey}");
        Discord.SendMessage(data);
    }

    public static void PiecePatch(string sendKey, bool hold, Piece sendPiece)
    {
        if (hold) return;
        if (PatchCheck(ref sendKey, out var creatorName, out var playerName)) return;
        DiscordWebhookData data = new($"$Guard {creatorName}", $"{playerName} {sendKey} {sendPiece.m_name}");
        Discord.SendMessage(data);
    }

    public static void ItemDropPatch(bool hold, ItemDrop itemDrop)
    {
        if (hold) return;
        string sendKey = "$ItemDropPickup";
        if (PatchCheck(ref sendKey, out var creatorName, out var playerName)) return;
        DiscordWebhookData data = new($"$Guard {creatorName}",
            $"{playerName} {sendKey} {itemDrop.m_itemData.m_shared.m_name}");
        Discord.SendMessage(data);
    }

    public static void PickablePatch(bool hold, Pickable pickable)
    {
        if (hold) return;
        var itemDrop = pickable.m_itemPrefab.GetComponent<ItemDrop>();
        if (!itemDrop) return;
        string sendKey = "$Pickable";
        if (PatchCheck(ref sendKey, out var creatorName, out var playerName)) return;
        DiscordWebhookData data = new($"$Guard {creatorName}",
            $"{playerName} {sendKey} {itemDrop.m_itemData.m_shared.m_name}");
        Discord.SendMessage(data);
    }

    public static void PatchTelleport(Player player, TeleportWorld teleportWorld)
    {
        if (!player) return;
        string sendKey = "$Teleport";
        if (PatchCheck(ref sendKey, out var creatorName, out var playerName)) return;
        DiscordWebhookData data = new($"$Guard {creatorName}", $"{playerName} {sendKey} {teleportWorld.GetText()}");
        Discord.SendMessage(data);
    }

    public static void FireplacePatch(bool hold, Fireplace fireplace)
    {
        if (hold) return;
        string sendKey = "$FireplaceFill";
        if (PatchCheck(ref sendKey, out var creatorName, out var playerName)) return;
        DiscordWebhookData data = new($"$Guard {creatorName}", $"{playerName} {sendKey} {fireplace.m_name}");
        Discord.SendMessage(data);
    }

    public static void ChairPatch(bool hold, Chair chair)
    {
        if (hold) return;
        string sendKey = "$Chair";
        if (PatchCheck(ref sendKey, out var creatorName, out var playerName)) return;
        DiscordWebhookData data = new($"$Guard {creatorName}", $"{playerName} {sendKey} {chair.m_name}");
        Discord.SendMessage(data);
    }

    private static bool PatchCheck(ref string sendKey, out string creatorName, out string playerName)
    {
        creatorName = "-none-";
        playerName = "-none-";
        if (!_self.canSendWebHook) return true;
        if (!Helper.GetCurrentAreaOwnerName(out creatorName)) return true;
        bool flag = Helper.CheckAccess(out _);
        if (flag) return true;
        playerName = Helper.GetPlayerName();
        if (!sendKey.StartsWith("$")) sendKey = "$" + sendKey;
        return false;
    }
}