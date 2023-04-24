using BepInEx.Bootstrap;
using DiscordWebhook;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DiscordWard.Plugin;

namespace DiscordWard;

[HarmonyPatch]
internal class WearNTearPatch
{
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Damage)), HarmonyPostfix]
    static void WearNTearDamagePatch(WearNTear __instance, HitData hit)
    {
        if (!Helper.GetCurrentAreaOwnerName(out string creatorName)) return;
        Character attacker = hit.GetAttacker();
        if (!attacker) return;
        string attackerMName = attacker.GetHoverName();

        string pieceName = __instance.m_piece.m_name;
        bool flag = Helper.CheckAccess(out _);
        if (flag) return;

        DiscordWebhookData data = new($"$Guard {creatorName}", $"");


        if (attacker.IsPlayer())
        {
            data.content = $"{Helper.GetPlayerName()} $DamageDestructible {pieceName}.";
        }
        else
        {
            data.content =
                $"{attackerMName} $MobDamageDestructible1 {pieceName} ";
            if (Helper.NameOfNearestPlayerInRange(out string nearestPlayerName))
                data.content += $"$MobDamageDestructible {nearestPlayerName}";
        }

        if (_self.canSendWebHook)
        {
            Discord.SendMessage(data);
        }
    }

    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Destroy)), HarmonyPostfix]
    static void WearNTearDestroyPatch(WearNTear __instance)
    {
        if (!Helper.GetCurrentAreaOwnerName(out string creatorName)) return;

        string pieceName = __instance.m_piece.m_name;
        bool flag = Helper.CheckAccess(out _);
        if (flag || Utils.DistanceXZ(Player.m_localPlayer.transform.position, __instance.transform.position) > 5)
            return; //TODO: Destroy detonation range
        string playerName = Helper.GetPlayerName();

        DiscordWebhookData data = new($"$Guard {creatorName}", $"{pieceName} $DestroyDestructible {playerName}");

        if (!_self.canSendWebHook) return;
        Discord.SendMessage(data);
    }
}