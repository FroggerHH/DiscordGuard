using System;
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
            if (SceneManager.GetActiveScene().name != "main") return;
            Discord.SendStartMessage();

            ZRoutedRpc.instance.Register(nameof(Discord.SendWebhook),
                new Action<long, string>(Discord.RPC_SendWebhook));
            ZRoutedRpc.instance.Register(nameof(Discord.RecuestWebhook),
                new Action<long>(Discord.RPC_RecuestWebhook));
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Start)), HarmonyPostfix]
        static void PlayerRecuestWebhookOnStart()
        {
            if (SceneManager.GetActiveScene().name != "main") return;
            if (!ZNet.m_isServer) Discord.RecuestWebhook();
        }
    }
}