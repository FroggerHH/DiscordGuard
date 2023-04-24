using System.Linq;
using System;
using DiscordWebhook;
using UnityEngine;
using static DiscordWard.Plugin;
using static Marketplace.Modules.TerritorySystem.TerritorySystem_DataTypes;
using Random = System.Random;
using Market_API;
using Marketplace.Modules.TerritorySystem;

namespace DiscordWard;

public static class Helper
{
    public static Player NearestPlayerInRange(GameObject to, float range)
    {
        Player current = null;
        float oldDistance = range;
        foreach (Player o in Player.m_players)
        {
            if (!o) continue;
            float dist = Vector3.Distance(to.transform.position, o.transform.position);
            if (dist < oldDistance)
            {
                current = o;
                oldDistance = dist;
            }
        }

        return current;
    }

    public static bool NameOfNearestPlayerInRange(out string name)
    {
        name = "--none--";
        Player currentPlayer = null;
        Vector3 to = Vector3.zero;
        float range = 0;
        if (current != null)
        {
            to = current.transform.position;
            range = current.m_radius;
        }
        else
        {
            var territory = Helper.GetCurrentTerritory();
            if (territory != null)
            {
                to = territory.Pos3D();
                range = territory.Radius;
            }
        }

        if (to == Vector3.zero) return false;
        float oldDistance = range;
        foreach (Player o in Player.m_players)
        {
            if (!o) continue;
            float dist = Vector3.Distance(to, o.transform.position);
            if (dist < oldDistance)
            {
                currentPlayer = o;
                oldDistance = dist;
            }
        }

        if (currentPlayer)
        {
            name = currentPlayer.GetPlayerName();
            return true;
        }

        return false;
    }

    public static bool GetCurrentAreaOwnerName(out string ownerName)
    {
        if (GetCurrentZoneName(out ownerName)) return true;
        if (CurrentVANILAAreaOwnerName(out ownerName)) return true;

        return false;
    }

    private static bool GetCurrentZoneName(out string territoryName)
    {
        territoryName = "-none-";
        if (!Marketplace_API.IsInstalled()) return false;
        var territory = GetCurrentTerritory();
        if (territory == null) return false;
        territoryName = territory.RawName();
        if (territoryName == "-none-") return false;
        return true;
    }

    private static bool CurrentVANILAAreaOwnerName(out string ownerName)
    {
        PrivateArea area = Plugin.current;
        ownerName = "-none-";
        if (!area || !area.m_nview || !Player.m_localPlayer) return false;
        var zdo = area.m_nview.GetZDO();
        if (zdo == null) return false;

        ownerName = zdo.GetString("creatorName");
        if (ownerName == "-none-") return false;
        return true;
    }

    public static string GetPlayerName()
    {
        if (Player.m_localPlayer) return Player.m_localPlayer.GetPlayerName();
        return "-none-";
    }

    public static bool CheckAccess(out bool isOwner)
    {
        isOwner = false;
        if (current)
        {
            isOwner = current.m_piece.IsCreator();
            bool access = PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, false, true);
            if (!access)
            {
                lastPrivateType = PrivateType.Ward;
                lastPrivateName = current.m_nview.GetZDO().GetString("creatorName");
                return false;
            }
        }

        var territory = GetCurrentTerritory();
        if (territory == null) return true;
        lastPrivateType = PrivateType.Zone;
        lastPrivateName = territory.RawName();
        var ownerTerrit = territory.IsOwner();
        return ownerTerrit;
    }

    private static Territory GetCurrentTerritory()
    {
        return TerritorySystem_Main_Client.CurrentTerritory;
    }

    public static void SimplePatch(string sendKey, bool hold, Player player)
    {
        if (hold) return;
        if (PatchCheck(ref sendKey, out var creatorName, out var playerName, player)) return;
        DiscordWebhookData data = new(creatorName, $"{playerName} {sendKey}");
        Discord.SendMessage(data);
    }

    public static void PiecePatch(string sendKey, bool hold, Piece sendPiece, Player player)
    {
        if (hold) return;
        if (PatchCheck(ref sendKey, out var creatorName, out var playerName, player)) return;
        DiscordWebhookData data = new(creatorName, $"{playerName} {sendKey} {sendPiece.m_name}");
        Discord.SendMessage(data);
    }

    public static void ItemDropPatch(bool hold, ItemDrop itemDrop, Player player)
    {
        if (hold) return;
        var itemName = itemDrop.m_itemData.m_shared.m_name;
        if (itemName.Contains("@") || itemName.Contains("attack")) return;
        string sendKey = "$ItemDropPickup";
        if (PatchCheck(ref sendKey, out var creatorName, out var playerName, player)) return;
        DiscordWebhookData data = new(creatorName,
            $"{playerName} {sendKey} {itemName}");
        Discord.SendMessage(data);
    }

    public static void PickablePatch(bool hold, Pickable pickable, Player player)
    {
        if (hold) return;
        var itemDrop = pickable.m_itemPrefab.GetComponent<ItemDrop>();
        if (!itemDrop) return;
        string sendKey = "$Pickable";
        if (PatchCheck(ref sendKey, out var creatorName, out var playerName, player)) return;
        DiscordWebhookData data = new(creatorName,
            $"{playerName} {sendKey} {itemDrop.m_itemData.m_shared.m_name}");
        Discord.SendMessage(data);
    }

    public static void PatchTelleport(Player player, TeleportWorld teleportWorld)
    {
        if (!player) return;
        string sendKey = "$Teleport";
        if (PatchCheck(ref sendKey, out var creatorName, out var playerName, player)) return;
        DiscordWebhookData data = new(creatorName, $"{playerName} {sendKey} {teleportWorld.GetText()}");
        Discord.SendMessage(data);
    }

    public static void FireplacePatch(bool hold, Fireplace fireplace, Player player)
    {
        if (hold) return;
        string sendKey = "$FireplaceFill";
        if (PatchCheck(ref sendKey, out var creatorName, out var playerName, player)) return;
        DiscordWebhookData data = new(creatorName, $"{playerName} {sendKey} {fireplace.m_name}");
        Discord.SendMessage(data);
    }

    public static void ChairPatch(bool hold, Chair chair, Player player)
    {
        if (hold) return;
        string sendKey = "$Chair";
        if (PatchCheck(ref sendKey, out var creatorName, out var playerName, player)) return;
        DiscordWebhookData data = new(creatorName, $"{playerName} {sendKey} {chair.m_name}");
        Discord.SendMessage(data);
    }

    private static bool PatchCheck(ref string sendKey, out string username, out string playerName, Player player)
    {
        username = "-none-";
        playerName = "-none-";
        if(player == null) return false;
        if(player != Player.m_localPlayer) return false;
        if (!_self.canSendWebHook) return true;
        if (!Helper.GetCurrentAreaOwnerName(out username)) return true;
        bool flag = Helper.CheckAccess(out _);
        if (flag) return true;
        playerName = player.GetPlayerName();
        if (!sendKey.StartsWith("$")) sendKey = "$" + sendKey;
        username = lastPrivateType == PrivateType.Ward ? $"$Guard {username}" : $"$Zone {username}";

        return false;
    }

    public static string RandomString(int length)
    {
        try
        {
        }
        catch (Exception e)
        {
            DebugError(e.Message, true);
            throw;
        }

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        Random random = new Random();
        var randomString = new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        return randomString;
    }
}