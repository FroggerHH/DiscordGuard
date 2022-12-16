using BepInEx.Bootstrap;
using DiscordWebhook;
using HarmonyLib;
using UnityEngine;
using static DiscordGuard.Plugin;

namespace DiscordGuard
{
    internal class Patch
    {
        #region PlayerStart
        [HarmonyPatch(typeof(Player), nameof(Player.SetLocalPlayer)), HarmonyPostfix]
        static void PlayerPatch()
        {
            if(!ZNet.instance || ZNet.instance.IsServer())
            {
                return;
            }

            if(Chainloader.PluginInfos.ContainsKey("server_devcommands"))
            {
                _self.DebugError("Hey, admin! If you want to test the mod on yourself, remove the devcommands mod. Thank you for using my mod ;)", false);
            }

            _self.Config.Reload();
        }
        #endregion

        #region PrivateAreaAwake
        [HarmonyPatch(typeof(PrivateArea), nameof(PrivateArea.Awake)), HarmonyPostfix]
        static void PrivateAreaAddComponentPatch(PrivateArea __instance)
        {
            GameObject prepab = __instance.gameObject;
            if(!prepab.GetComponent<DiscordGuard>())
            {
                prepab.AddComponent<DiscordGuard>();
            }
        }
        #endregion

        #region WearNTearDamage
        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Damage)), HarmonyPostfix]
        static void WearNTearDamagePatch(WearNTear __instance, HitData hit)
        {
            string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
            string playerName = Player.m_localPlayer?.GetPlayerName();
            if(creatorName == string.Empty) return;

            string pieceName = __instance.m_piece.m_name;
            bool flag = true; 
            if(Player.m_localPlayer)
            {
                flag = PrivateArea.CheckAccess(Player.m_localPlayer.transform.position);
            }

            DiscordWebhookData data = new()
            {
                username = $"$Guard $WardNickPrefix {creatorName} $WardNickPostfix",
            };

            Character attacker = hit.GetAttacker();

            if(attacker == null)
            {
                return;
            }

            if(attacker.IsPlayer() && currentDiscordGuard)
            {
                data.content = $"{playerName} $DamageDestructible {pieceName}.";
            }
            else if(attacker.IsPlayer())
            {
                data.content = $"{playerName} $NoWardDamageDestructible {pieceName} $1NoWardDamageDestructible";
            }

            if(!attacker.IsPlayer() && currentDiscordGuard)
            {
                data.content = $"{hit.GetAttacker().m_name} $MobDamageDestructible1 {pieceName} $MobDamageDestructible {playerName}.";
            }
            else if(!attacker.IsPlayer())
            {
                data.content = $"{hit.GetAttacker().m_name} $NoWardMobDamageDestructible1 {pieceName} $NoWardMobDamageDestructible.";
            }

            if(!flag && _self.canSendWebHook)
            {
                Discord.SendDiscordMessage(data);
                _self.canSendWebHook = false;
            }
            if(_self.canSendLogWebHook)
            {
                Discord.SendDiscordMessage(data, true); _self.canSendLogWebHook = false;
            }
        }
        #endregion

        #region WearNTearDestroy
        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Destroy)), HarmonyPostfix]
        static void WearNTearDestroyPatch(WearNTear __instance)
        {
            if(Player.m_localPlayer)
            {
                string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                string playerName = Player.m_localPlayer?.GetPlayerName();

                string pieceName = __instance.m_piece.m_name;
                bool flag = PrivateArea.CheckAccess(Player.m_localPlayer.transform.position) || playerName == creatorName;
                DiscordWebhookData data = new()
                {
                    username = $"$Guard $WardNickPrefix {creatorName} $WardNickPostfix ",
                    content = $"{pieceName} $DestroyDestructible {playerName}."
                };

                if(creatorName != string.Empty)
                {
                    if(!flag && _self.canSendWebHook)
                    {
                        Discord.SendDiscordMessage(data);
                        _self.canSendWebHook = false;
                    }
                }
                else if(_self.canSendLogWebHook)
                {
                    if(string.IsNullOrEmpty(creatorName))
                    {
                        return;
                    }
                    else
                    {
                        data.username = $"$Log $Guard $WardNickPrefix {creatorName} $WardNickPostfix";
                    }

                    Discord.SendDiscordMessage(data, true);
                    _self.canSendLogWebHook = false;
                }
            }
        }
        #endregion

        #region Door
        [HarmonyPatch(typeof(Door), nameof(Door.Interact)), HarmonyPrefix]
        static void DoorPrefixPatch(bool hold)
        {
            string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
            string playerName = Player.m_localPlayer?.GetPlayerName();

            bool flag = hold || PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, false) || playerName == creatorName;

            DiscordWebhookData data = new()
            {
                username = $" $Guard $WardNickPrefix {creatorName} $WardNickPostfix ",
                content = $"{playerName} $Door"
            };

            if(creatorName != string.Empty)
            {
                if(!flag && _self.canSendWebHook)
                {
                    Discord.SendDiscordMessage(data);
                    _self.canSendWebHook = false;
                }
            }
            else if(_self.canSendLogWebHook)
            {
                if(string.IsNullOrEmpty(creatorName))
                {
                    return;
                }
                else
                {
                    data.username = $"$Log $Guard $WardNickPrefix {creatorName} $WardNickPostfix";
                }

                Discord.SendDiscordMessage(data, true); _self.canSendLogWebHook = false;
            }

            return;
        }
        [HarmonyPatch(typeof(Door), nameof(Door.Interact)), HarmonyPostfix]
        static void DoorPatch(bool hold, bool __result, Door __instance)
        {
            string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
            string playerName = Player.m_localPlayer?.GetPlayerName();
            bool flag = __result || hold || !__instance.CanInteract() || playerName == creatorName;

            DiscordWebhookData data = new()
            {
                username = $" $Guard $WardNickPrefix {creatorName} $WardNickPostfix ",
                content = $"{playerName} $Door"
            };

            if(creatorName != string.Empty)
            {
                if(!flag && _self.canSendWebHook)
                {
                    Discord.SendDiscordMessage(data);
                    _self.canSendWebHook = false;
                }
            }
            else if(_self.canSendLogWebHook)
            {
                if(string.IsNullOrEmpty(creatorName))
                {
                    return;
                }
                else
                {
                    data.username = $"$Log $Guard $WardNickPrefix {creatorName} $WardNickPostfix";
                }

                Discord.SendDiscordMessage(data, true); _self.canSendLogWebHook = false;
            }

            return;
        }
        #endregion

        #region Beehive
        [HarmonyPatch(typeof(Beehive), nameof(Beehive.Interact)), HarmonyPrefix]
        static void BeehivePatch(bool repeat)
        {

            string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
            string playerName = Player.m_localPlayer?.GetPlayerName();
            bool flag = repeat || PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, false) || playerName == creatorName;

            DiscordWebhookData data = new()
            {
                username = $"$Guard $WardNickPrefix {creatorName} $WardNickPostfix ",
                content = $"{playerName} $Honey!"
            };

            if(creatorName != string.Empty)
            {
                if(!flag && _self.canSendWebHook)
                {
                    Discord.SendDiscordMessage(data);
                    _self.canSendWebHook = false;
                }
            }
            else if(_self.canSendLogWebHook)
            {
                if(string.IsNullOrEmpty(creatorName))
                {
                    return;
                }
                else
                {
                    data.username = $"$Log $Guard $WardNickPrefix {creatorName} $WardNickPostfix";
                }

                Discord.SendDiscordMessage(data, true); _self.canSendLogWebHook = false;
            }

            return;
        }
        [HarmonyPatch(typeof(Beehive), nameof(Beehive.Interact)), HarmonyPostfix]
        static void BeehivePostfixPatch(bool __result, bool repeat, Beehive __instance)
        {
            string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
            string playerName = Player.m_localPlayer?.GetPlayerName();
            bool flag = __result || repeat || playerName == creatorName;

            DiscordWebhookData data = new()
            {
                username = $"$Guard $WardNickPrefix {creatorName} $WardNickPostfix ",
                content = $"{playerName} $Honey!"
            };

            if(creatorName != string.Empty)
            {
                if(!flag && _self.canSendWebHook)
                {
                    Discord.SendDiscordMessage(data);
                    _self.canSendWebHook = false;
                }
            }
            else if(_self.canSendLogWebHook)
            {
                if(string.IsNullOrEmpty(creatorName))
                {
                    return;
                }
                else
                {
                    data.username = $"$Log $Guard $WardNickPrefix {creatorName} $WardNickPostfix";
                }

                Discord.SendDiscordMessage(data, true); _self.canSendLogWebHook = false;
            }

            return;
        }
        #endregion

        #region Chest
        [HarmonyPatch(typeof(Container), nameof(Container.Interact)), HarmonyPrefix]
        static void ContainerPatch(bool hold)
        {
            string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
            string playerName = Player.m_localPlayer?.GetPlayerName();
            bool flag = hold || PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, false) || playerName == creatorName;

            DiscordWebhookData data = new()
            {
                username = $"$Guard $WardNickPrefix {creatorName} $WardNickPostfix ",
                content = $"{playerName} $Chest!"
            };

            if(creatorName != string.Empty)
            {
                if(!flag && _self.canSendWebHook)
                {
                    Discord.SendDiscordMessage(data);
                    _self.canSendWebHook = false;
                }
            }
            else if(_self.canSendLogWebHook)
            {
                if(string.IsNullOrEmpty(creatorName))
                {
                    return;
                }
                else
                {
                    data.username = $"$Log $Guard $WardNickPrefix {creatorName} $WardNickPostfix";
                }

                Discord.SendDiscordMessage(data, true); _self.canSendLogWebHook = false;
            }

            return;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
        static void ContainerPatch(bool hold, bool __result)
        {
            string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
            string playerName = Player.m_localPlayer?.GetPlayerName();
            bool flag = __result || hold || playerName == creatorName;
            DiscordWebhookData data = new()
            {
                username = $"$Guard $WardNickPrefix {creatorName} $WardNickPostfix ",
                content = $"{playerName} $Chest!"
            };

            if(creatorName != string.Empty)
            {
                if(!flag && _self.canSendWebHook)
                {
                    Discord.SendDiscordMessage(data);
                    _self.canSendWebHook = false;
                }
            }
            else if(_self.canSendLogWebHook)
            {
                if(string.IsNullOrEmpty(creatorName))
                {
                    return;
                }
                else
                {
                    data.username = $"$Log $Guard $WardNickPrefix {creatorName} $WardNickPostfix";
                }

                Discord.SendDiscordMessage(data, true); _self.canSendLogWebHook = false;
            }

            return;
        }
        #endregion

        #region CraftingStation
        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Interact)), HarmonyPostfix]
        static void CraftingStationPatchPostfix(bool __result, CraftingStation __instance)
        {
            string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
            string playerName = Player.m_localPlayer?.GetPlayerName();
            string pieceName = __instance.m_name;
            bool flag = __result || __instance.GetComponent<Piece>().IsCreator() || playerName == creatorName;
            DiscordWebhookData data = new()
            {
                username = $"$Guard $WardNickPrefix {creatorName} $WardNickPostfix ",
                content = $"{playerName} $CraftingStation {pieceName}!"
            };

            if(!flag && _self.canSendWebHook)
            {
                Discord.SendDiscordMessage(data);
                _self.canSendWebHook = false;

            }
            Discord.SendDiscordMessage(data, true);
            _self.canSendLogWebHook = false;
        }
        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Interact)), HarmonyPrefix]
        static bool CraftingStationPrefixPatch(CraftingStation __instance)
        {
            string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
            string playerName = Player.m_localPlayer?.GetPlayerName();

            string pieceName = __instance.m_name;
            bool flag = PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, false) || playerName == creatorName;

            DiscordWebhookData data = new()
            {
                username = $"$Guard $WardNickPrefix {creatorName} $WardNickPostfix ",
                content = $"{playerName} $CraftingStation {pieceName}!"
            };
            if(creatorName != string.Empty)
            {
                if(!flag && _self.canSendWebHook)
                {
                    Discord.SendDiscordMessage(data);
                    _self.canSendWebHook = false;
                }
            }
            else if(_self.canSendLogWebHook)
            {
                if(string.IsNullOrEmpty(creatorName))
                {
                    return true;
                }
                else
                {
                    data.username = $"$Log $Guard $WardNickPrefix {creatorName} $WardNickPostfix";
                }

                Discord.SendDiscordMessage(data, true); _self.canSendLogWebHook = false;
            }

            if(!flag && preventCrafting)
            {
                return false;
            }

            return true;
        }
        #endregion

        #region ItemStand
        [HarmonyPatch(typeof(ItemStand), nameof(ItemStand.Interact)), HarmonyPrefix]
        static void ItemStandPatch(bool hold)
        {
            string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
            string playerName = Player.m_localPlayer?.GetPlayerName();
            bool flag = hold || PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, false) || playerName == creatorName;

            DiscordWebhookData data = new();
            data.username = $"$Guard $WardNickPrefix {creatorName} $WardNickPostfix ";
            data.content = $"{playerName} $ItemStand!";

            if(creatorName != string.Empty)
            {
                if(!flag && _self.canSendWebHook)
                {
                    Discord.SendDiscordMessage(data);
                    _self.canSendWebHook = false;
                }
            }
            else if(_self.canSendLogWebHook)
            {
                if(string.IsNullOrEmpty(creatorName))
                {
                    return;
                }
                else
                {
                    data.username = $"$Log $Guard $WardNickPrefix {creatorName} $WardNickPostfix";
                }

                Discord.SendDiscordMessage(data, true); _self.canSendLogWebHook = false;
            }

            return;
        }
        [HarmonyPatch(typeof(ItemStand), nameof(ItemStand.Interact)), HarmonyPostfix]
        static void ItemStandPatch(bool hold, bool __result)
        {
            string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
            string playerName = Player.m_localPlayer?.GetPlayerName();
            bool flag = __result || hold || playerName == creatorName;
            DiscordWebhookData data = new()
            {
                username = $"$Guard $WardNickPrefix {creatorName} $WardNickPostfix ",
                content = $"{playerName} $ItemStand!"
            };

            if(creatorName != string.Empty)
            {
                if(!flag && _self.canSendWebHook)
                {
                    Discord.SendDiscordMessage(data);
                    _self.canSendWebHook = false;
                }
            }
            else if(_self.canSendLogWebHook)
            {
                if(string.IsNullOrEmpty(creatorName))
                {
                    return;
                }
                else
                {
                    data.username = $"$Log $Guard $WardNickPrefix {creatorName} $WardNickPostfix";
                }

                Discord.SendDiscordMessage(data, true); _self.canSendLogWebHook = false;
            }
            return;
        }
        #endregion

        #region Sign
        [HarmonyPatch(typeof(Sign), nameof(Sign.Interact)), HarmonyPrefix]
        static bool SignPrefixPatch(bool hold)
        {
            string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
            string playerName = Player.m_localPlayer?.GetPlayerName();
            bool flag = hold || PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, false) || playerName == creatorName;

            DiscordWebhookData data = new DiscordWebhookData
            {
                username = $"$Guard $WardNickPrefix {creatorName} $WardNickPostfix ",
                content = $"{playerName} $Sign!"
            };
            if(creatorName != string.Empty)
            {
                if(!flag && _self.canSendWebHook)
                {
                    Discord.SendDiscordMessage(data);
                    _self.canSendWebHook = false;
                }
            }
            else if(_self.canSendLogWebHook)
            {
                if(string.IsNullOrEmpty(creatorName))
                {
                    return true;
                }
                else
                {
                    data.username = $"$Log $Guard $WardNickPrefix {creatorName} $WardNickPostfix";
                }

                Discord.SendDiscordMessage(data, true); _self.canSendLogWebHook = false;
            }

            return true;
        }
        [HarmonyPatch(typeof(Sign), nameof(Sign.Interact)), HarmonyPostfix]
        static void SignPatch(bool hold, bool __result)
        {
            string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
            string playerName = Player.m_localPlayer?.GetPlayerName();
            bool flag = __result || hold || playerName == creatorName;
            DiscordWebhookData data = new DiscordWebhookData
            {
                username = $"$Guard $WardNickPrefix {creatorName} $WardNickPostfix ",
                content = $"{playerName} $Sign!"
            };

            if(creatorName != string.Empty)
            {
                if(!flag && _self.canSendWebHook)
                {
                    Discord.SendDiscordMessage(data);
                    _self.canSendWebHook = false;
                }
            }
            else if(_self.canSendLogWebHook)
            {
                if(string.IsNullOrEmpty(creatorName))
                {
                    return;
                }
                else
                {
                    data.username = $"$Log $Guard $WardNickPrefix {creatorName} $WardNickPostfix";
                }

                Discord.SendDiscordMessage(data, true); _self.canSendLogWebHook = false;
            }

            return;
        }
        #endregion

        #region Teleport
        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Interact)), HarmonyPrefix]
        static void TeleportInteractPrefixPatch(bool hold)
        {
            string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
            string playerName = Player.m_localPlayer?.GetPlayerName();
            bool flag = hold || PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, false) || playerName == creatorName;

            DiscordWebhookData data = new DiscordWebhookData
            {
                username = $"$Guard $WardNickPrefix {creatorName} $WardNickPostfix ",
                content = $"{playerName} $TeleportInteract!"
            };
            if(creatorName != string.Empty)
            {
                if(!flag && _self.canSendWebHook)
                {
                    Discord.SendDiscordMessage(data);
                    _self.canSendWebHook = false;
                }
            }
            else if(_self.canSendLogWebHook)
            {
                if(string.IsNullOrEmpty(creatorName))
                {
                    return;
                }
                else
                {
                    data.username = $"$Log $Guard $WardNickPrefix {creatorName} $WardNickPostfix";
                }

                Discord.SendDiscordMessage(data, true); _self.canSendLogWebHook = false;
            }

            return;
        }
        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Interact)), HarmonyPostfix]
        static void TeleportInteractPatch(bool hold, bool __result)
        {
            string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
            string playerName = Player.m_localPlayer?.GetPlayerName();
            bool flag = __result || hold || playerName == creatorName;

            DiscordWebhookData data = new DiscordWebhookData
            {
                username = $"$Guard $WardNickPrefix {creatorName} $WardNickPostfix ",
                content = $"{playerName} $TeleportInteract!"
            };

            if(creatorName != string.Empty)
            {
                if(!flag && _self.canSendWebHook)
                {
                    Discord.SendDiscordMessage(data);
                    _self.canSendWebHook = false;
                }
            }
            else if(_self.canSendLogWebHook)
            {
                if(string.IsNullOrEmpty(creatorName))
                {
                    return;
                }
                else
                {
                    data.username = $"$Log $Guard $WardNickPrefix {creatorName} $WardNickPostfix";
                }

                Discord.SendDiscordMessage(data, true); _self.canSendLogWebHook = false;
            }

            return;
        }
        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Teleport)), HarmonyPostfix]
        static void TeleportPatch(TeleportWorld __instance)
        {
            string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
            string playerName = Player.m_localPlayer?.GetPlayerName();
            string portalTag = __instance.GetComponent<ZNetView>().GetZDO().GetString("tag");
            bool flag = PrivateArea.CheckAccess(Player.m_localPlayer.transform.position) || playerName == creatorName;

            DiscordWebhookData data = new()
            {
                username = $"$Guard $WardNickPrefix {creatorName} $WardNickPostfix ",
                content = $"{playerName} $Teleport {portalTag}!"
            };
            if(creatorName != string.Empty)
            {
                if(!flag && _self.canSendWebHook)
                {
                    Discord.SendDiscordMessage(data);
                    _self.canSendWebHook = false;
                }
            }
            else if(_self.canSendLogWebHook)
            {
                if(string.IsNullOrEmpty(creatorName))
                {
                    return;
                }
                else
                {
                    data.username = $"$Log $Guard $WardNickPrefix {creatorName} $WardNickPostfix";
                }

                Discord.SendDiscordMessage(data, true); _self.canSendLogWebHook = false;
            }
        }
        #endregion

        #region ItemDrop
        [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Pickup)), HarmonyPrefix]
        static bool ItemDropPickupPatch(ItemDrop __instance)
        {

            bool item = __instance.m_itemData.m_shared?.m_icons?.Length >= 1; if(!item)
            {
                return true;
            }

            string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
            string playerName = Player.m_localPlayer?.GetPlayerName();
            string itemName = __instance.m_itemData.m_shared.m_name;
            bool flag = PrivateArea.CheckAccess(Player.m_localPlayer.transform.position) || playerName == creatorName;

            DiscordWebhookData data = new()
            {
                username = $"$Guard $WardNickPrefix {creatorName} $WardNickPostfix ",
                content = $"{playerName} $ItemDropPickup {itemName}!"
            };

            if(creatorName != string.Empty)
            {
                if(!flag && _self.canSendWebHook)
                {
                    Discord.SendDiscordMessage(data);
                    _self.canSendWebHook = false;
                }
            }
            else if(_self.canSendLogWebHook)
            {
                if(string.IsNullOrEmpty(creatorName))
                {
                    return true;
                }
                else
                {
                    data.username = $"$Log $Guard $WardNickPrefix {creatorName} $WardNickPostfix";
                }

                Discord.SendDiscordMessage(data, true); _self.canSendLogWebHook = false;
            }

            if(!flag && preventItemDropPickup)
            {
                return false;
            }

            return true;
        }
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Pickup)), HarmonyPrefix]
        static bool ItemDropAutoPickupPatch(GameObject go)
        {
            if(!Player.m_localPlayer)
            {
                return true;
            }

            bool item = go.GetComponent<ItemDrop>()?.m_itemData?.m_shared.m_icons?.Length >= 1;
            if(!item)
            {
                return true;
            }

            string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
            string playerName = Player.m_localPlayer?.GetPlayerName();
            string itemName = Localization.instance.Localize(go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name);
            bool flag = PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, false) || playerName == creatorName;

            DiscordWebhookData data = new()
            {
                username = $"$Guard $WardNickPrefix {creatorName} $WardNickPostfix ",
                content = $"{playerName} $ItemDropAutoPickup {itemName}!"
            };

            if(creatorName != string.Empty)
            {
                if(!flag && _self.canSendWebHook)
                {
                    Discord.SendDiscordMessage(data);
                    _self.canSendWebHook = false;
                }
            }
            else if(_self.canSendLogWebHook)
            {
                if(string.IsNullOrEmpty(creatorName))
                {
                    return true;
                }
                else
                {
                    data.username = $"$Log $Guard $WardNickPrefix {creatorName} $WardNickPostfix";
                }

                Discord.SendDiscordMessage(data, true); _self.canSendLogWebHook = false;
            }

            if(!flag && preventItemDropPickup)
            {
                return false;
            }

            return true;
        }
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Pickup)), HarmonyPostfix]
        static void ItemDropAutoPickupPostfixPatch(bool __result, GameObject go)
        {
            bool item = go.GetComponent<ItemDrop>()?.m_itemData?.m_shared.m_icons?.Length >= 1;
            if(!item)
            {
                return;
            }

            string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
            string playerName = Player.m_localPlayer?.GetPlayerName();
            string itemName = Localization.instance.Localize(go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name);
            bool flag = __result || playerName == creatorName;

            DiscordWebhookData data = new()
            {
                username = $"$Guard $WardNickPrefix {creatorName} $WardNickPostfix ",
                content = $"{playerName} $ItemDropAutoPickup {itemName}!"
            };

            if(creatorName != string.Empty)
            {
                if(!flag && _self.canSendWebHook)
                {
                    Discord.SendDiscordMessage(data);
                    _self.canSendWebHook = false;
                }
            }
            else if(_self.canSendLogWebHook)
            {
                if(string.IsNullOrEmpty(creatorName))
                {
                    return;
                }
                else
                {
                    data.username = $"$Log $Guard $WardNickPrefix {creatorName} $WardNickPostfix";
                }

                Discord.SendDiscordMessage(data, true); _self.canSendLogWebHook = false;
            }

        }
        #endregion

        #region Pickable
        [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact)), HarmonyPrefix]
        static bool PickablePatch(Pickable __instance)
        {
            string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
            string playerName = Player.m_localPlayer?.GetPlayerName();
            string itemName = __instance.m_itemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
            bool flag = PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, false) || playerName == creatorName;

            DiscordWebhookData data = new()
            {
                username = $"$Guard $WardNickPrefix {creatorName} $WardNickPostfix",
                content = $"{playerName} $Pickable1 {itemName}!"
            };

            if(creatorName != string.Empty)
            {
                if(!flag && _self.canSendWebHook)
                {
                    Discord.SendDiscordMessage(data);
                    _self.canSendWebHook = false;
                }
            }
            else if(_self.canSendLogWebHook)
            {
                if(string.IsNullOrEmpty(creatorName))
                {
                    return true;
                }
                else
                {
                    data.username = $"$Log $Guard $WardNickPrefix {creatorName} $WardNickPostfix";
                }

                Discord.SendDiscordMessage(data, true); _self.canSendLogWebHook = false;
            }

            if(!flag && preventPickablePickup)
            {
                return false;
            }

            return true;
        }
        [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact)), HarmonyPostfix]
        static void PickablePostfixPatch(bool __result, bool repeat, Pickable __instance)
        {
            string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
            string playerName = Player.m_localPlayer?.GetPlayerName();
            string itemName = __instance.m_itemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
            bool flag = __result || repeat || playerName == creatorName;

            DiscordWebhookData data = new DiscordWebhookData
            {
                username = $"$Guard $WardNickPrefix {creatorName} $WardNickPostfix",
                content = $"{playerName} $Pickable1 {itemName}!"
            };

            if(creatorName != string.Empty)
            {
                if(!flag && _self.canSendWebHook)
                {
                    Discord.SendDiscordMessage(data);
                    _self.canSendWebHook = false;
                }
            }
            else if(_self.canSendLogWebHook)
            {
                if(string.IsNullOrEmpty(creatorName))
                {
                    return;
                }
                else
                {
                    data.username = $"$Log $Guard $WardNickPrefix {creatorName} $WardNickPostfix";
                }

                Discord.SendDiscordMessage(data, true); _self.canSendLogWebHook = false;
            }

            return;
        }
        #endregion

        #region GuardInteract
        [HarmonyPatch(typeof(PrivateArea), nameof(PrivateArea.Interact)), HarmonyPostfix]
        static void VANILAGuardInteractPatch(PrivateArea __instance, Humanoid human, bool hold, bool alt)
        {
            string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
            string playerName = Player.m_localPlayer?.GetPlayerName();
            if(creatorName == string.Empty) return;
            bool flag = PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, false);
            DiscordWebhookData data = new()
            {
                username = $"$Guard $WardNickPrefix {creatorName} $WardNickPostfix",
                content = $"{playerName} $VANILAGuardInteract!"
            };

            if(!flag && _self.canSendWebHook)
            {
                Discord.SendDiscordMessage(data);
                _self.canSendWebHook = false;
            }

            Discord.SendDiscordMessage(data, true);
            _self.canSendLogWebHook = false;
        }
        #endregion
    }
}
