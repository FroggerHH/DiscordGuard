using BepInEx;
using BepInEx.Configuration;
using DiscordWebhook;
using fastJSON;
using HarmonyLib;
using LocalizationManager;
using ServerSync;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#pragma warning disable CS0618
namespace DiscordGuard
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    internal class Plugin : BaseUnityPlugin
    {
        #region values
        internal const string ModName = "DiscordGuard", ModVersion = "0.1.0", ModGUID = "com.Frogger." + ModName;
        internal static Harmony harmony = new(ModGUID);

        internal static DiscordGuard currentDiscordGuard;
        internal static List<DiscordGuard> guards = new();
        internal static Plugin _self;
        internal bool canSendWebHook = false;
        internal bool canSendLogWebHook = false;
        internal bool enterGuard = false;
        internal bool leftGuard = true;
        #endregion
         
        #region ConfigSettings
        #region tools
        static string ConfigFileName = "com.Frogger.DiscordGuard.cfg";
        DateTime LastConfigChange;
        public static readonly ConfigSync configSync = new(ModName) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        private static ConfigEntry<Toggle> serverConfigLocked = null!;
        public static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = _self.Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }
        private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        void SetCfgValue<T>(Action<T> setter, ConfigEntry<T> config)
        {
            setter(config.Value);
            config.SettingChanged += (_, _) => setter(config.Value);
        }
        public enum Toggle
        {
            On = 1,
            Off = 0
        }
        #endregion
        #region configs
        static ConfigEntry<string> moderatorUrlConfig;
        static ConfigEntry<string> logrUrlConfig;
        static ConfigEntry<string> languageServerConfig;
        static ConfigEntry<Toggle> preventItemDropPickupConfig;
        static ConfigEntry<Toggle> preventPickablePickupConfig;
        static ConfigEntry<Toggle> preventCraftingConfig;
        static ConfigEntry<float> webHookTimerConfig;
        static ConfigEntry<float> logWebHookTimerConfig;
        #endregion
        #region config values
        internal static string moderatorUrl = "";
        public static string languageServer = "English";
        //public static Localization localization = new();
        internal static string logrUrl = "";
        internal static bool preventItemDropPickup = false;
        internal static bool preventPickablePickup = false;
        internal static bool preventCrafting = false;
        internal static float webHookTimer = 2.5f;
        internal static float logWebHookTimer = 2f;
        #endregion
        #endregion

        private void Awake()
        {
            _self = this;
            //localization.SetupLanguage("English");

            Config.SaveOnConfigSet = false;

            Localizer.Load();

            JSON.Parameters = new JSONParameters
            {
                UseExtensions = false,
                SerializeNullValues = false,
                DateTimeMilliseconds = false,
                UseUTCDateTime = true,
                UseOptimizedDatasetSchema = true,
                UseValuesOfEnums = true
            };
            #region config
            serverConfigLocked = config("Main", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
            configSync.AddLockingConfigEntry(serverConfigLocked);

            moderatorUrlConfig = config("Urls", "moderatorUrl", "", new ConfigDescription("Url of the moderator's webhook", null, new ConfigurationManagerAttributes { HideSettingName = true, HideDefaultButton = true }), true);
            logrUrlConfig = config("Urls", "logrUrl", "", new ConfigDescription("It differs in that all messages come here, both from the actions of players on foreign shores and in their own.", null, new ConfigurationManagerAttributes { HideSettingName = true, HideDefaultButton = true }), true);
            languageServerConfig = config("Main", "server language", "English", "The language in which the moderator receives notifications.", true);
            preventItemDropPickupConfig = config("Main", "preventItemDropPickup", Toggle.Off, "If you use a mod that already prevents ItemDropPickup in guard, you can leave Off. WORKS ONLY WITH VANILA WARD", true);
            preventPickablePickupConfig = config("Main", "preventPickablePickup", Toggle.Off, "If you use a mod that already prevents PickablePickup in guard, you can leave Off. WORKS ONLY WITH VANILA WARD", true);
            preventCraftingConfig = config("Main", "preventCrafting", Toggle.Off, "If you use a mod that already prevents using Crafting in guard, you can leave Off. WORKS ONLY WITH VANILA WARD", true);
            webHookTimerConfig = config("Main", "webHookTimer", 0.5f, "This is the minimum time interval between sending webhooks.", true);//\nNeed to restart to update value.
            logWebHookTimerConfig = config("Main", "Log webHookTimer", 2f, "This is the minimum time interval between sending log webhooks.", true);

            SetupWatcher();
            Config.SettingChanged += (_, _) => { UpdateConfiguration(); };
            Config.ConfigReloaded += (_, _) => { UpdateConfiguration(); };

            Config.Save();
            #endregion

            InvokeRepeating("UdateCurrentDiscordGuard", 6, 5);
            InvokeRepeating("WebHookTimer", webHookTimer, webHookTimer);
            InvokeRepeating("LogWebHookTimer", logWebHookTimer, logWebHookTimer);
            InvokeRepeating("EnterLeftGuard", 10, 4);

            harmony.PatchAll(typeof(Patch));
            Config.Reload();
            Discord.SendStartWebhook();

        }

        private void SetupWatcher()
        {
            FileSystemWatcher fileSystemWatcher = new(Paths.ConfigPath, ConfigFileName);
            fileSystemWatcher.Changed += ConfigChanged;
            fileSystemWatcher.IncludeSubdirectories = true;
            fileSystemWatcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            fileSystemWatcher.EnableRaisingEvents = true;
        }
        void ConfigChanged(object sender, FileSystemEventArgs e)
        {
            if((DateTime.Now - LastConfigChange).TotalSeconds <= 5.0)
            {
                return;
            }
            LastConfigChange = DateTime.Now;
            try
            {
                Config.Reload();
            }
            catch
            {
                DebugError("Can't reload Config", true);
            }
        }

        private void UpdateConfiguration()
        {
            //if(Player.m_localPlayer)
            //{
            moderatorUrl = moderatorUrlConfig.Value;
            logrUrl = logrUrlConfig.Value;
            languageServer = languageServerConfig.Value;
            //localization.SetupLanguage(languageServer);
            preventItemDropPickup = preventItemDropPickupConfig.Value == Toggle.On;
            preventPickablePickup = preventPickablePickupConfig.Value == Toggle.On;
            preventCrafting = preventCraftingConfig.Value == Toggle.On;
            webHookTimer = webHookTimerConfig.Value;
            logWebHookTimer = logWebHookTimerConfig.Value;

            if(moderatorUrl != string.Empty) moderatorUrl = moderatorUrl.Replace(" ", "");

            if(logrUrl != string.Empty) logrUrl = logrUrl.Replace(" ", "");

            CancelInvoke("WebHookTimer");
            CancelInvoke("LogWebHookTimer");
            InvokeRepeating("WebHookTimer", webHookTimer, webHookTimer);
            InvokeRepeating("LogWebHookTimer", logWebHookTimer, logWebHookTimer);

            Debug("Configuration Received");
            //}
        }

        private void WebHookTimer()
        {
            canSendWebHook = true;
        }
        private void LogWebHookTimer()
        {
            canSendLogWebHook = true;
        }

        private void UdateCurrentDiscordGuard()
        {
            bool isFocused = Application.isFocused;

            if(Player.m_localPlayer && isFocused)
            {
                currentDiscordGuard = null;
                float oldDistance = 9999;
                foreach(DiscordGuard guard in guards)
                {
                    if(guard)
                    {
                        float dist = Vector3.Distance(Player.m_localPlayer.transform.position, guard.transform.position);
                        if(dist < oldDistance && dist <= 40)
                        {
                            currentDiscordGuard = guard;
                            oldDistance = dist;
                        }
                    }
                }
            }
        }

        private void EnterLeftGuard()
        {
            bool isFocused = Application.isFocused;

            if(!isFocused || !canSendWebHook || !Player.m_localPlayer)
            {
                return;
            }

            bool flag = PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, false, false);
            if(!flag && !enterGuard && currentDiscordGuard && moderatorUrl != "")
            {
                enterGuard = true;
                leftGuard = false;
                string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                string playerName = Player.m_localPlayer?.GetPlayerName();
                if(creatorName == string.Empty)
                {
                    return;
                }

                _self.Debug($"{ playerName} $InGuard {creatorName}!");
                DiscordWebhookData data = new()
                {
                    username = $"{creatorName} $Guard",
                    content = $"{playerName} $InGuard!"
                };
                Discord.SendDiscordMessage(data);
            }
            else if(flag && !leftGuard)
            {
                enterGuard = false;
                leftGuard = true;
                string creatorName = currentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                string playerName = Player.m_localPlayer?.GetPlayerName();
                if(creatorName == string.Empty)
                {
                    return;
                }

                _self.Debug($"{playerName} $LeftGuard  {creatorName} !");
                DiscordWebhookData data = new()
                {
                    username = $"{creatorName} $Guard",
                    content = $"{playerName} $LeftGuard!"
                };
                Discord.SendDiscordMessage(data);
            }
        }

        public void Debug(string msg)
        {
            if(Localization.instance != null)
            {
                Logger.LogInfo(Localization.instance.Localize(msg));
            }
            else
            {
                Logger.LogInfo(msg);
            }
        }
        public void SimpleDebug(string msg)
        {
            Logger.LogInfo(msg);
        }
        public void DebugError(string msg, bool showWriteToDev)
        {
            if(showWriteToDev)
            {
                msg += "Write to the developer and moderator if this happens often.";
            }

            Logger.LogError(msg);
        }
        public void DebugWarning(string msg)
        {
            Logger.LogWarning($"{msg} Write to the developer and moderator if this happens often.");
        }

        /* [HarmonyPatch]
         public static class OLDPatсh
         {
             [HarmonyPostfix]
             [HarmonyPatch(typeof(Player), nameof(Player.SetLocalPlayer))]
             static void PlayerPatch()
             {
                 if (!ZNet.instance || ZNet.instance.IsServer())
                 {
                     return;
                 }

                 _self.UpdateConfiguration();
             }
             [HarmonyPostfix]
             [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
             static void ZNetScenePatch()
             {
                 GameObject Thorward = ZNetScene.instance.GetPrefab("Thorward");
                 if (Thorward)
                 {
                     _self.Debug("Thorward");
                     Thorward.AddComponent<DiscordGuard>();
                 }
             }

             [HarmonyPostfix]
             [HarmonyPatch(typeof(PrivateArea), "Awake")]
             static void PrivateAreaAddComponentPatch(PrivateArea __instance)
             {
                 GameObject prepab = __instance.gameObject;
                 if (!prepab.GetComponent<DiscordGuard>())
                 {
                     prepab.AddComponent<DiscordGuard>();
                 }
             }
             [HarmonyPostfix]
             [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Damage))]
             static void WearNTearDamagePatch(WearNTear __instance, HitData hit)
             {
                 bool flag = PrivateArea.CheckAccess(Player.m_localPlayer.transform.position);
                 if (!flag)
                 {
                     if (_self.canSendWebHook)
                     {
                         string creatorName = CurrentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                         string playerName = Player.m_localPlayer?.GetPlayerName(); 
                         string pieceName = __instance.m_piece.m_name;

                         DiscordWebhookData data = new DiscordWebhookData
                         {
                             username = $"$Guard $WardNickPrefix{creatorName}$WardNickPostfix ",
                             content = $"{playerName} $DamageDestructible {pieceName}"
                         };
                         _self.Debug($"{playerName} $DamageDestructible {pieceName} $InTheWardOf $ConsoleNickPrefix {creatorName} $ConsoleNickPostfix!");

                         Discord.SendWebhook(data);
                         _self.canSendWebHook = false;
                     }
                 }

             }
             [HarmonyPostfix]
             [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Destroy))]
             static void WearNTearDestroyPatch(WearNTear __instance)
             {
                 if (Player.m_localPlayer)
                 {
                     bool flag = PrivateArea.CheckAccess(Player.m_localPlayer.transform.position);

                     if (!flag)
                     {
                         if (_self.canSendWebHook && !IsPermitted)
                         {
                             string creatorName = CurrentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                             string playerName = Player.m_localPlayer?.GetPlayerName(); 
                             string pieceName = __instance.m_piece.m_name;

                             DiscordWebhookData data = new DiscordWebhookData
                             {
                                 username = $"$Guard $WardNickPrefix{creatorName}$WardNickPostfix ",
                                 content = $"{playerName} $DestroyDestructible {pieceName}"
                             };
                             _self.Debug($"{playerName} $DestroyDestructible {pieceName} $InTheWardOf $ConsoleNickPrefix {creatorName} $ConsoleNickPostfix!");

                             Discord.SendWebhook(data);
                             _self.canSendWebHook = false;
                         }
                     }
                 }
             }
             [HarmonyPrefix]
             [HarmonyPatch(typeof(Door), nameof(Door.Interact))]
             static void DoorPrefixPatch(bool hold)
             {
                 if (hold || PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, false))
                 {
                     return;
                 }
                 else
                 {
                     if (_self.canSendWebHook && !IsPermitted)
                     {
                         string creatorName = CurrentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                         string playerName = Player.m_localPlayer?.GetPlayerName(); 

                         DiscordWebhookData data = new DiscordWebhookData
                         {
                             username = $" $Guard $WardNickPrefix{creatorName}$WardNickPostfix ",
                             content = $"{playerName} $Door"
                         };
                         _self.Debug($"{playerName} $Door $InTheWardOf $ConsoleNickPrefix {creatorName} $ConsoleNickPostfix!");

                         Discord.SendWebhook(data);
                         _self.canSendWebHook = false;
                     }
                 }

                 return;
             }
             [HarmonyPostfix]
             [HarmonyPatch(typeof(Door), nameof(Door.Interact))]
             static void DoorPatch(bool hold, bool __result, Door __instance)
             {
                 _self.Debug("DoorPatch HarmonyPostfix " + __result);
                 if (__result || hold || !__instance.CanInteract())
                 {
                     return;
                 }
                 else
                 {
                     if (_self.canSendWebHook && !IsPermitted)
                     {
                         string creatorName = CurrentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                         string playerName = Player.m_localPlayer?.GetPlayerName(); 

                         DiscordWebhookData data = new DiscordWebhookData
                         {
                             username = $" $Guard $WardNickPrefix{creatorName}$WardNickPostfix ",
                             content = $"{playerName} $Door"
                         };
                         _self.Debug($"{playerName} $Door $InTheWardOf $ConsoleNickPrefix {creatorName} $ConsoleNickPostfix!");

                         Discord.SendWebhook(data);
                         _self.canSendWebHook = false;
                     }
                 }

                 return;
             }
             [HarmonyPrefix]
             [HarmonyPatch(typeof(Beehive), nameof(Beehive.Interact))]
             static bool BeehivePatch(bool repeat)
             {
                 if (repeat || PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, false))
                 {
                     return true;
                 }
                 else
                 {
                     if (_self.canSendWebHook && !IsPermitted)
                     {
                         string creatorName = CurrentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                         string playerName = Player.m_localPlayer?.GetPlayerName(); 

                         DiscordWebhookData data = new DiscordWebhookData
                         {
                             username = $"$Guard $WardNickPrefix{creatorName}$WardNickPostfix ",
                             content = $"{playerName} $Honey!"
                         };
                         _self.Debug($"{playerName} $Honey $InTheWardOf  {creatorName} !");

                         Discord.SendWebhook(data);
                         _self.canSendWebHook = false;
                     }
                 }

                 return true;
             }
             [HarmonyPostfix]
             [HarmonyPatch(typeof(Beehive), nameof(Beehive.Interact))]
             static void BeehivePostfixPatch(bool __result, bool repeat, Beehive __instance)
             {
                 if (__result || repeat)
                 {
                     return;
                 }
                 else
                 {
                     if (_self.canSendWebHook && !IsPermitted)
                     {
                         string creatorName = CurrentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                         string playerName = Player.m_localPlayer?.GetPlayerName(); 

                         DiscordWebhookData data = new DiscordWebhookData
                         {
                             username = $"$Guard $WardNickPrefix{creatorName}$WardNickPostfix ",
                             content = $"{playerName} $Honey!"
                         };
                         _self.Debug($"{playerName} $Honey $InTheWardOf  {creatorName} !");

                         Discord.SendWebhook(data);
                         _self.canSendWebHook = false;
                     }
                 }

                 return;
             }
             [HarmonyPrefix]
             [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
             static void ContainerPatch(bool hold)
             {
                 if (hold || PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, false))
                 {
                     return;
                 }
                 else
                 {
                     if (_self.canSendWebHook && !IsPermitted)
                     {
                         string creatorName = CurrentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                         string playerName = Player.m_localPlayer?.GetPlayerName(); 


                         DiscordWebhookData data = new DiscordWebhookData
                         {
                             username = $"$Guard $WardNickPrefix{creatorName}$WardNickPostfix ",
                             content = $"{playerName} $Chest!"
                         };
                         _self.Debug($"{playerName} $Chest $InTheWardOf $ConsoleNickPrefix {creatorName} $ConsoleNickPostfix!");

                         Discord.SendWebhook(data);
                         _self.canSendWebHook = false;
                     }
                 }

                 return;
             }
             [HarmonyPostfix]
             [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
             static void ContainerPatch(bool hold, bool __result)
             {
                 if (__result || hold)
                 {
                     return;
                 }
                 else
                 {
                     if (_self.canSendWebHook && !IsPermitted)
                     {
                         string creatorName = CurrentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                         string playerName = Player.m_localPlayer?.GetPlayerName(); 


                         DiscordWebhookData data = new DiscordWebhookData
                         {
                             username = $"$Guard $WardNickPrefix{creatorName}$WardNickPostfix ",
                             content = $"{playerName} $Chest!"
                         };
                         _self.Debug($"{playerName} $Chest $InTheWardOf $ConsoleNickPrefix {creatorName} $ConsoleNickPostfix!");

                         Discord.SendWebhook(data);
                         _self.canSendWebHook = false;
                     }
                 }

                 return;
             }
             [HarmonyPostfix]
             [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Interact))]
             static void CraftingStationPatch(bool __result, CraftingStation __instance)
             {
                 if (__result || __instance.GetComponent<Piece>().IsCreator())
                 {
                     return;
                 }

                 if (_self.canSendWebHook && !IsPermitted)
                 {
                     string creatorName = CurrentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                     string playerName = Player.m_localPlayer?.GetPlayerName(); 
                     string pieceName = __instance.m_name;

                     DiscordWebhookData data = new DiscordWebhookData
                     {
                         username = $" $Guard $WardNickPrefix{creatorName}$WardNickPostfix ",
                         content = $"{playerName} $CraftingStation {pieceName}!"
                     };
                     _self.Debug($"{playerName} $CraftingStation $InTheWardOf $ConsoleNickPrefix {creatorName} $ConsoleNickPostfix!");
                     _self.Debug($"CraftingStation HarmonyPostfix");

                     Discord.SendWebhook(data);
                     _self.canSendWebHook = false;
                 }

                 return;
             }
             [HarmonyPrefix]
             [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Interact))]
             static bool CraftingStationPrefixPatch(CraftingStation __instance)
             {
                 bool flag = PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, false);
                 if (flag)
                 {
                     return true;
                 }
                 else
                 {
                     if (_self.canSendWebHook && !IsPermitted)
                     {
                         string creatorName = CurrentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                         string playerName = Player.m_localPlayer?.GetPlayerName(); 
                         string pieceName = __instance.m_name;

                         DiscordWebhookData data = new DiscordWebhookData
                         {
                             username = $" $Guard $WardNickPrefix{creatorName}$WardNickPostfix ",
                             content = $"{playerName} $CraftingStation {pieceName}!"
                         };
                         _self.Debug($"{playerName} $CraftingStation $InTheWardOf $ConsoleNickPrefix {creatorName} $ConsoleNickPostfix!");
                         _self.Debug($"CraftingStation HarmonyPrefix");


                         Discord.SendWebhook(data);
                         _self.canSendWebHook = false;
                     }
                     if (preventCrafting)
                     {
                         return false;
                     }
                 }

                 return true;
             }
             [HarmonyPrefix]
             [HarmonyPatch(typeof(ItemStand), nameof(ItemStand.Interact))]
             static void ItemStandPatch(bool hold)
             {
                 if (hold || PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, false))
                 {
                     return;
                 }
                 else
                 {
                     if (_self.canSendWebHook && !IsPermitted)
                     {
                         string creatorName = CurrentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                         string playerName = Player.m_localPlayer?.GetPlayerName(); 

                         DiscordWebhookData data = new();
                         data.username = $"$Guard $WardNickPrefix{creatorName}$WardNickPostfix ";
                         data.content = $"{playerName} $ItemStand!";
                         _self.Debug($"{playerName} $ItemStand $InTheWardOf $ConsoleNickPrefix {creatorName} $ConsoleNickPostfix");

                         Discord.SendWebhook(data);
                         _self.canSendWebHook = false;
                     }
                 }

                 return;
             }
             [HarmonyPostfix]
             [HarmonyPatch(typeof(ItemStand), nameof(ItemStand.Interact))]
             static void ItemStandPatch(bool hold, bool __result)
             {
                 if (__result || hold)
                 {
                     return;
                 }
                 else
                 {
                     if (_self.canSendWebHook && !IsPermitted)
                     {
                         string creatorName = CurrentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                         string playerName = Player.m_localPlayer?.GetPlayerName(); 

                         DiscordWebhookData data = new();
                         data.username = $"$Guard $WardNickPrefix{creatorName}$WardNickPostfix ";
                         data.content = $"{playerName} $ItemStand!";
                         _self.Debug($"{playerName} $ItemStand $InTheWardOf $ConsoleNickPrefix {creatorName} $ConsoleNickPostfix");

                         Discord.SendWebhook(data);
                         _self.canSendWebHook = false;
                     }
                 }

                 return;
             }
             [HarmonyPostfix]
             [HarmonyPatch(typeof(Sign), nameof(Sign.Interact))]
             static void SignPatch(bool hold, bool __result)
             {
                 if (__result || hold)
                 {
                     return;
                 }
                 else
                 {
                     if (_self.canSendWebHook && !IsPermitted)
                     {
                         string creatorName = CurrentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                         string playerName = Player.m_localPlayer?.GetPlayerName(); 

                         DiscordWebhookData data = new DiscordWebhookData
                         {
                             username = $"$Guard $WardNickPrefix{creatorName}$WardNickPostfix ",
                             content = $"{playerName} $Sign!"
                         };
                         _self.Debug($"{playerName} $Sign $InTheWardOf $ConsoleNickPrefix {creatorName} $ConsoleNickPostfix!");

                         Discord.SendWebhook(data);
                         _self.canSendWebHook = false;
                     }
                 }

                 return;
             }
             [HarmonyPrefix]
             [HarmonyPatch(typeof(Sign), nameof(Sign.Interact))]
             static bool SignPrefixPatch(bool hold)
             {
                 if (hold || PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, false))
                 {
                     return true;
                 }
                 else
                 {
                     if (_self.canSendWebHook && !IsPermitted)
                     {
                         string creatorName = CurrentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                         string playerName = Player.m_localPlayer?.GetPlayerName(); 

                         DiscordWebhookData data = new DiscordWebhookData
                         {
                             username = $"$Guard $WardNickPrefix{creatorName}$WardNickPostfix ",
                             content = $"{playerName} $Sign!"
                         };
                         _self.Debug($"{playerName} $Sign $InTheWardOf $ConsoleNickPrefix {creatorName} $ConsoleNickPostfix!");

                         Discord.SendWebhook(data);
                         _self.canSendWebHook = false;
                     }
                 }

                 return true;
             }
             [HarmonyPostfix]
             [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Interact))]
             static void TeleportInteractPatch(bool hold, bool __result)
             {
                 if (__result || hold)
                 {
                     return;
                 }
                 else
                 {
                     if (_self.canSendWebHook && !IsPermitted)
                     {
                         string creatorName = CurrentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                         string playerName = Player.m_localPlayer?.GetPlayerName(); 

                         DiscordWebhookData data = new DiscordWebhookData
                         {
                             username = $"$Guard $WardNickPrefix{creatorName}$WardNickPostfix ",
                             content = $"{playerName} $TeleportInteract!"
                         };
                         _self.Debug($"{playerName} $TeleportInteract $InTheWardOf $ConsoleNickPrefix {creatorName} $ConsoleNickPostfix!");

                         Discord.SendWebhook(data);
                         _self.canSendWebHook = false;
                     }
                 }

                 return;
             }
             [HarmonyPrefix]
             [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Interact))]
             static void TeleportInteractPrefixPatch(bool hold)
             {
                 if (hold || PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, false))
                 {
                     return;
                 }
                 else
                 {
                     if (_self.canSendWebHook && !IsPermitted)
                     {
                         string creatorName = CurrentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                         string playerName = Player.m_localPlayer?.GetPlayerName(); 

                         DiscordWebhookData data = new DiscordWebhookData
                         {
                             username = $"$Guard $WardNickPrefix{creatorName}$WardNickPostfix ",
                             content = $"{playerName} $TeleportInteract!"
                         };
                         _self.Debug($"{playerName} $TeleportInteract $InTheWardOf $ConsoleNickPrefix {creatorName} $ConsoleNickPostfix!");

                         Discord.SendWebhook(data);
                         _self.canSendWebHook = false;
                     }
                 }

                 return;
             }
             [HarmonyPostfix]
             [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Teleport))]
             static void TeleportPatch(TeleportWorld __instance)
             {
                 bool flag = PrivateArea.CheckAccess(Player.m_localPlayer.transform.position);
                 if (!flag)
                 {
                     string creatorName = CurrentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                     string playerName = Player.m_localPlayer?.GetPlayerName(); 
                     string portalTag = __instance.GetComponent<ZNetView>().GetZDO().GetString("tag");

                     DiscordWebhookData data = new DiscordWebhookData
                     {
                         username = $"$Guard $WardNickPrefix{creatorName}$WardNickPostfix ",
                         content = $"{playerName} $Teleport {portalTag}!"
                     };
                     _self.Debug($"{playerName} $Teleport {portalTag} $InTheWardOf $ConsoleNickPrefix {creatorName} $ConsoleNickPostfix!");

                     Discord.SendWebhook(data);
                     _self.canSendWebHook = false;
                 }
             }
             [HarmonyPrefix]
             [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Pickup))]
             static bool ItemDropPickupPatch(ItemDrop __instance)
             {
                 bool flag = PrivateArea.CheckAccess(Player.m_localPlayer.transform.position);

                 if (flag)
                 {
                     return true;
                 }
                 else
                 {
                     if (_self.canSendWebHook && !IsPermitted)
                     {
                         bool item = __instance.m_itemData.m_shared?.m_icons?.Length >= 1;
                         if (!item)
                         {
                             return true;
                         }

                         string creatorName = CurrentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                         string playerName = Player.m_localPlayer?.GetPlayerName(); 
                         string itemName = __instance.m_itemData.m_shared.m_name;

                         DiscordWebhookData data = new DiscordWebhookData
                         {
                             username = $"$Guard $WardNickPrefix{creatorName}$WardNickPostfix ",
                             content = $"{playerName} $ItemDropPickup {itemName}!"
                         };
                         _self.Debug($"{playerName} $ItemDropPickup {itemName} $InTheWardOf $ConsoleNickPrefix {creatorName} $ConsoleNickPostfix!");

                         Discord.SendWebhook(data);
                         _self.canSendWebHook = false;
                     }
                     if (preventItemDropPickup)
                     {
                         return false;
                     }
                 }


                 return true;
             }
             [HarmonyPrefix]
             [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Pickup))]
             static bool ItemDropAutoPickupPatch(GameObject go)
             {
                 if (!Player.m_localPlayer)
                 {
                     return true;
                 }

                 bool flag = PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, false);

                 if (flag)
                 {
                     return true;
                 }
                 else
                 {
                     if (_self.canSendWebHook && !IsPermitted)
                     {
                         bool item = go.GetComponent<ItemDrop>()?.m_itemData?.m_shared.m_icons?.Length >= 1;
                         if (!item)
                         {
                             return true;
                         }

                         string creatorName = CurrentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                         string playerName = Player.m_localPlayer?.GetPlayerName(); 
                         string itemName = Localization.instance.Localize(go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name);

                         DiscordWebhookData data = new DiscordWebhookData
                         {
                             username = $"$Guard $WardNickPrefix{creatorName}$WardNickPostfix ",
                             content = $"{playerName} $ItemDropAutoPickup {itemName}!"
                         };
                         _self.Debug($"{playerName} $ItemDropAutoPickup {itemName} $InTheWardOf $ConsoleNickPrefix {creatorName} $ConsoleNickPostfix!");

                         Discord.SendWebhook(data);
                         _self.canSendWebHook = false;
                     }
                     if (preventItemDropPickup)
                     {
                         return false;
                     }
                 }

                 return true;
             }
             [HarmonyPostfix]
             [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Pickup))]
             static void ItemDropAutoPickupPostfixPatch(bool __result, GameObject go)
             {
                 bool flag = __result;

                 if (flag)
                 {
                     return;
                 }
                 else
                 {
                     if (_self.canSendWebHook && !IsPermitted)
                     {
                         bool item = go.GetComponent<ItemDrop>()?.m_itemData?.m_shared.m_icons?.Length >= 1;
                         if (!item)
                         {
                             return;
                         }

                         string creatorName = CurrentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                         string playerName = Player.m_localPlayer?.GetPlayerName(); 
                         string itemName = Localization.instance.Localize(go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name);

                         DiscordWebhookData data = new DiscordWebhookData
                         {
                             username = $"$Guard $WardNickPrefix{creatorName}$WardNickPostfix ",
                             content = $"{playerName} $ItemDropAutoPickup {itemName}!"
                         };
                         _self.Debug($"{playerName} $ItemDropAutoPickup {itemName} $InTheWardOf $ConsoleNickPrefix {creatorName} $ConsoleNickPostfix!");

                         Discord.SendWebhook(data);
                         _self.canSendWebHook = false;
                     }

                 }

             }
             [HarmonyPrefix]
             [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
             static bool PickablePatch(Pickable __instance)
             {
                 bool access = PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, false);

                 if (access)
                 {
                     return true;
                 }
                 else
                 {
                     if (_self.canSendWebHook && !IsPermitted)
                     {
                         string creatorName = CurrentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                         string playerName = Player.m_localPlayer?.GetPlayerName(); 
                         string itemName = __instance.m_itemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;

                         DiscordWebhookData data = new DiscordWebhookData
                         {
                             username = $"$Guard $WardNickPrefix{creatorName}$WardNickPostfix",
                             content = $"{playerName} $Pickable1 {itemName} $Pickable2!"
                         };
                         _self.Debug($"{playerName} $Pickable1 {itemName} $Pickable2 $InTheWardOf $ConsoleNickPrefix {creatorName} $ConsoleNickPostfix!");

                         Discord.SendWebhook(data);
                         _self.canSendWebHook = false;
                     }
                     if (preventPickablePickup)
                     {
                         return false;
                     }

                 }

                 return true;
             }
             [HarmonyPostfix]
             [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
             static void PickablePostfixPatch(bool __result, bool repeat, Pickable __instance)
             {
                 if (__result || repeat)
                 {
                     return;
                 }
                 else
                 {
                     if (_self.canSendWebHook && !IsPermitted)
                     {
                         string creatorName = CurrentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                         string playerName = Player.m_localPlayer?.GetPlayerName(); 
                         string itemName = __instance.m_itemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;

                         DiscordWebhookData data = new DiscordWebhookData
                         {
                             username = $"$Guard $WardNickPrefix{creatorName}$WardNickPostfix",
                             content = $"{playerName} $Pickable1 {itemName} $Pickable2!"
                         };
                         _self.Debug($"{playerName} $Pickable1 {itemName} $Pickable2 $InTheWardOf $ConsoleNickPrefix {creatorName} $ConsoleNickPostfix!");

                         Discord.SendWebhook(data);
                         _self.canSendWebHook = false;
                     }
                 }

                 return;
             }
             [HarmonyPostfix]
             [HarmonyPatch(typeof(PrivateArea), nameof(PrivateArea.Interact))]
             static bool VANILAGuardInteractPatch(bool __result, PrivateArea __instance, Humanoid human, bool hold, bool alt)
             {
                 DiscordGuard discordGuard = __instance.GetComponent<DiscordGuard>();
                 if (discordGuard)
                 {
                     if (!PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, false))
                     {
                         string creatorName = CurrentDiscordGuard?.nview?.GetZDO()?.GetString("creatorName");
                         string playerName = Player.m_localPlayer?.GetPlayerName(); 

                         DiscordWebhookData data = new DiscordWebhookData
                         {
                             username = $"$Guard $WardNickPrefix{creatorName}$WardNickPostfix",
                             content = $"{playerName} $VANILAGuardInteract!"
                         };
                         _self.Debug($"{playerName} $VANILAGuardInteract $InTheWardOf $ConsoleNickPrefix {creatorName} $ConsoleNickPostfix!");

                         Discord.SendWebhook(data);
                         _self.canSendWebHook = false;
                     }
                 }

                 return true;
             }
         }*/
    }
}