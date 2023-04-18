using BepInEx.Bootstrap;
using DiscordWebhook;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DiscordGuard.Plugin;

namespace DiscordGuard;

[HarmonyPatch]
internal class DoorPatch
{
    [HarmonyPatch(typeof(Door), nameof(Door.Interact)), HarmonyPostfix]
    public static void DoorPatchInteract(Door __instance)
    {
        if (!Helper.GetCurrentAreaOwnerName(out string creatorName)) return;
        bool flag = Helper.CheckAccess();
        if (flag) return; //TODO: Destroy detonation range
        string playerName = Helper.GetPlayerName();

        DiscordWebhookData data = new($"$Guard {creatorName}", $"{playerName} $Door");

        if (!_self.canSendWebHook) return;
        Discord.SendMessage(data);
    }
}