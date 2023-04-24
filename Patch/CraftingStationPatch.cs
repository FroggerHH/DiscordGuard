using BepInEx.Bootstrap;
using DiscordWebhook;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DiscordWard.Plugin;

namespace DiscordWard;

[HarmonyPatch]
internal class CraftingStationPatch
{
    [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Interact)), HarmonyPostfix]
    public static void CraftingStationPatchInteract(CraftingStation __instance, bool repeat, Humanoid user)
    {
        Helper.PiecePatch("$CraftingStation", repeat, __instance.GetComponent<Piece>(), user as Player);
    }
}