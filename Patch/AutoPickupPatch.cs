using BepInEx.Bootstrap;
using DiscordWebhook;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DiscordWard.Plugin;

namespace DiscordWard;

[HarmonyPatch]
internal class AutoPickupPatch
{
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Pickup)), HarmonyPostfix]
    public static void ItemDropPatchInteract(Humanoid __instance, GameObject go, bool autoequip, bool autoPickupDelay)
    {
        var itemDrop = go.GetComponent<ItemDrop>();
        bool flag = !itemDrop || itemDrop.m_itemData == null || itemDrop.m_itemData.m_shared.m_icons == null || itemDrop.m_itemData.m_shared.m_icons.Length < 1;
        if(flag) return;
        
        Helper.ItemDropPatch(false, itemDrop, __instance as Player);
    }
}