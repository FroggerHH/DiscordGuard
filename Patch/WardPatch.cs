using BepInEx.Bootstrap;
using DiscordWebhook;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DiscordWard.Plugin;

namespace DiscordWard;

[HarmonyPatch]
internal class WardPatch
{
    [HarmonyPatch(typeof(PrivateArea), nameof(PrivateArea.Interact)), HarmonyPostfix]
    public static void SignPatchInteract(PrivateArea __instance, bool hold, Humanoid human)
    {
        if (!sendGuardInteractMessagesConfig.Value) return;


        Helper.SimplePatch("$VANILAGuardInteract", hold, human as Player);
    }
}