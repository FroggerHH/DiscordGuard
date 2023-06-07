using BepInEx.Bootstrap;
using DiscordWebhook;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DiscordWard.Plugin;

namespace DiscordWard;

[HarmonyPatch]
internal class MapTablePatch
{
    [HarmonyPatch(typeof(MapTable), nameof(MapTable.OnRead)), HarmonyPostfix]
    public static void MapTableOnReadPatchOnRead(MapTable __instance, Humanoid user)
    {
        if (!sendMapTableMessagesConfig.Value) return;

        Helper.SimplePatch("$MapTableRead", false, user as Player);
    }

    [HarmonyPatch(typeof(MapTable), nameof(MapTable.OnWrite)), HarmonyPostfix]
    public static void MapTableOnReadPatchOnWrite(MapTable __instance, Humanoid user)
    {
        if (!sendMapTableMessagesConfig.Value) return;
        
        Helper.SimplePatch("$MapTableWrite", false, user as Player);
    }
}