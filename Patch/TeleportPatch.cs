using BepInEx.Bootstrap;
using DiscordWebhook;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DiscordWard.Plugin;

namespace DiscordWard;

[HarmonyPatch]
internal class TeleportPatch
{
    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Interact)), HarmonyPostfix]
    public static void TeleporEditTagPatch(TeleportWorld __instance, bool hold, Humanoid human)
    {
        if (!sendTeleportMessagesConfig.Value) return;


        Helper.SimplePatch("$TeleportInteract", hold, human as Player);
    }

    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Teleport)), HarmonyPostfix]
    public static void TeleportWorldPatchTeleport(TeleportWorld __instance, Player player)
    {
        if (!sendTeleportMessagesConfig.Value) return;


        Helper.PatchTelleport(player, __instance);
    }
}