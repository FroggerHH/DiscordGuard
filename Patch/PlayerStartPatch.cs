using BepInEx.Bootstrap;
using DiscordWebhook;
using HarmonyLib;
using UnityEngine;
using static DiscordGuard.Plugin;

namespace DiscordGuard
{
    internal class PlayerStartPatch
    {
        [HarmonyPatch(typeof(Player), nameof(Player.Start)), HarmonyPostfix]
        static void PlayerPatch()
        {

            _self.Config.Reload();
        }
    }
}
