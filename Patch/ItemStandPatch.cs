using BepInEx.Bootstrap;
using DiscordWebhook;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DiscordWard.Plugin;

namespace DiscordWard;

[HarmonyPatch]
internal class ItemStandPatch
{
    [HarmonyPatch(typeof(ItemStand), nameof(ItemStand.Interact)), HarmonyPostfix]
    public static void ItemStandPatchInteract(ItemStand __instance, bool hold)
    {
        Helper.SimplePatch("$ItemStand", hold);
    }
}