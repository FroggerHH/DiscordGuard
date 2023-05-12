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
        Character attacker = hit.GetAttacker();
        if (!attacker) return;
        string attackerMName = attacker.GetHoverName();
        string sendKey = string.Empty;
        if (Helper.PatchCheck(ref sendKey, out var username, out var _, Player.m_localPlayer)) return;

        if(!__instance.m_piece) return;
        string pieceName = __instance.m_piece.m_name;

        DiscordWebhookData data = new(username, $"");


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