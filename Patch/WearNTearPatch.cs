using BepInEx.Bootstrap;
using DiscordWebhook;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DiscordGuard.Plugin;

namespace DiscordGuard;

internal class WearNTearPatch
{
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Damage)), HarmonyPostfix]
    static void WearNTearDamagePatch(WearNTear __instance, HitData hit)
    {
        string creatorName = current?.nview?.GetZDO()?.GetString("creatorName");
        string playerName = Player.m_localPlayer?.GetPlayerName();
        if (creatorName == string.Empty) return;

        string pieceName = __instance.m_piece.m_name;
        bool flag = true;
        if (Player.m_localPlayer)
        {
            flag = PrivateArea.CheckAccess(Player.m_localPlayer.transform.position);
        }

        DiscordWebhookData data = new($"$Guard $WardNickPrefix {creatorName} $WardNickPostfix", "");

        Character attacker = hit.GetAttacker();

        if (attacker == null)
        {
            return;
        }

        if (attacker.IsPlayer() && current)
        {
            data.content = $"{playerName} $DamageDestructible {pieceName}.";
        }
        else if (attacker.IsPlayer())
        {
            data.content = $"{playerName} $NoWardDamageDestructible {pieceName} $1NoWardDamageDestructible";
        }

        if (!attacker.IsPlayer() && current)
        {
            data.content =
                $"{hit.GetAttacker().m_name} $MobDamageDestructible1 {pieceName} $MobDamageDestructible {playerName}.";
        }
        else if (!attacker.IsPlayer())
        {
            data.content =
                $"{hit.GetAttacker().m_name} $NoWardMobDamageDestructible1 {pieceName} $NoWardMobDamageDestructible.";
        }

        if (!flag && _self.canSendWebHook)
        {
            Discord.SendMessage(data);
            _self.canSendWebHook = false;
        }

        // if (_self.canSendLogWebHook)
        // {
        //     Discord.SendMessage(data, true);
        //     _self.canSendLogWebHook = false;
        // }
    }

    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Destroy)), HarmonyPostfix]
    static void WearNTearDestroyPatch(WearNTear __instance)
    {
        if (Player.m_localPlayer)
        {
            string creatorName = current?.nview?.GetZDO()?.GetString("creatorName");
            string playerName = Player.m_localPlayer?.GetPlayerName();

            string pieceName = __instance.m_piece.m_name;
            bool flag = PrivateArea.CheckAccess(Player.m_localPlayer.transform.position) || playerName == creatorName;
            DiscordWebhookData data = new($"$Guard $WardNickPrefix {creatorName} $WardNickPostfix ",
                $"{pieceName} $DestroyDestructible {playerName}.");

            if (creatorName != string.Empty)
            {
                if (!flag && _self.canSendWebHook)
                {
                    Discord.SendMessage(data);
                    _self.canSendWebHook = false;
                }
            }
            else if (_self.canSendLogWebHook)
            {
                if (string.IsNullOrEmpty(creatorName))
                {
                    return;
                }
                else
                {
                    data.username = $"$Log $Guard $WardNickPrefix {creatorName} $WardNickPostfix";
                }

                Discord.SendMessage(data, true);
                _self.canSendLogWebHook = false;
            }
        }
    }
}