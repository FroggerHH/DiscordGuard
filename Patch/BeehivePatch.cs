using BepInEx.Bootstrap;
using DiscordWebhook;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DiscordWard.Plugin;

namespace DiscordWard;

[HarmonyPatch]
internal class BeehivePatch
{
    [HarmonyPatch(typeof(Beehive), nameof(Beehive.Interact)), HarmonyPostfix]
    public static void BeehivePatchInteract(Beehive __instance, bool repeat, Humanoid character)
    {
        Helper.SimplePatch("$Honey", repeat, character as Player);
    }
}