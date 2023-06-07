using BepInEx.Bootstrap;
using DiscordWebhook;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DiscordWard.Plugin;

namespace DiscordWard;

[HarmonyPatch]
internal class DoorPatch
{
    [HarmonyPatch(typeof(Door), nameof(Door.Interact)), HarmonyPostfix]
    public static void DoorPatchInteract(Door __instance, bool hold, Humanoid character)
    {
        if (!sendDoorMessagesConfig.Value) return;

        Helper.SimplePatch("$Door", hold, character as Player);
    }
}