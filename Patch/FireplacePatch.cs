using BepInEx.Bootstrap;
using DiscordWebhook;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DiscordWard.Plugin;

namespace DiscordWard;

[HarmonyPatch]
internal class FireplacePatch
{
    [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.Interact)), HarmonyPostfix]
    public static void FireplacePatchInteract(Fireplace __instance, bool hold, Humanoid user)
    {
        if (!sendFireplaceMessagesConfig.Value) return;

        Helper.FireplacePatch(hold, __instance, user as Player);
    }
}