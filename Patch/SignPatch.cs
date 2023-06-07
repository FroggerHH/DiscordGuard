using BepInEx.Bootstrap;
using DiscordWebhook;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DiscordWard.Plugin;

namespace DiscordWard;

[HarmonyPatch]
internal class SignPatch
{
    [HarmonyPatch(typeof(Sign), nameof(Sign.Interact)), HarmonyPostfix]
    public static void SignPatchInteract(Sign __instance, bool hold, Humanoid character)
    {
        if (!sendSignMessagesConfig.Value) return;


        Helper.SimplePatch("$Sign", hold, character as Player);
    }
}