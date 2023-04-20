using BepInEx.Bootstrap;
using DiscordWebhook;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DiscordWard.Plugin;

namespace DiscordWard
{
    [HarmonyPatch]
    internal class ZNetSceneStartPatch
    {
        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPostfix]
        static void ZNetSceneAwakePatch()
        {
            if (SceneManager.GetActiveScene().name == "main") Discord.SendStartMessage();
        }
    }
}