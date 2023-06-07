using BepInEx.Bootstrap;
using DiscordWebhook;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DiscordWard.Plugin;

namespace DiscordWard;

[HarmonyPatch]
internal class ChairPatch
{
    [HarmonyPatch(typeof(Chair), nameof(Chair.Interact)), HarmonyPostfix]
    public static void ChairPatchInteract(Chair __instance, bool hold, Humanoid human)
    {
        if (!sendChairMessagesConfig.Value) return;

        Helper.ChairPatch(hold, __instance, human as Player);
    }
}