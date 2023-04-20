using BepInEx.Bootstrap;
using DiscordWebhook;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DiscordWard.Plugin;

namespace DiscordWard;

[HarmonyPatch]
internal class ContainerPatch
{
    [HarmonyPatch(typeof(Container), nameof(Container.Interact)), HarmonyPostfix]
    public static void ContainerPatchInteract(Beehive __instance, bool hold)
    {
        Helper.SimplePatch("$Chest", hold);
    }
}