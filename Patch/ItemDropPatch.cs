using BepInEx.Bootstrap;
using DiscordWebhook;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DiscordWard.Plugin;

namespace DiscordWard;

[HarmonyPatch]
internal class ItemDropPatch
{
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Interact)), HarmonyPostfix]
    public static void ItemDropPatchInteract(ItemDrop __instance, bool repeat)
    {
        Helper.ItemDropPatch(repeat, __instance);
    }
}