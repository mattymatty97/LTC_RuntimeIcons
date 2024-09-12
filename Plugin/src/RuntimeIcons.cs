using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RuntimeIcons.Dependency;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using RuntimeIcons.Components;
using RuntimeIcons.Patches;
using UnityEngine;

namespace RuntimeIcons
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("BMX.LobbyCompatibility", Flags: BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("ainavt.lc.lethalconfig", Flags: BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("mattymatty.MattyFixes", "1.1.21")]
    internal class RuntimeIcons : BaseUnityPlugin
    {
        internal static readonly ISet<Hook> Hooks = new HashSet<Hook>();
        internal static readonly Harmony Harmony = new Harmony(GUID);

        public static RuntimeIcons INSTANCE { get; private set; }

        public const string GUID = MyPluginInfo.PLUGIN_GUID;
        public const string NAME = MyPluginInfo.PLUGIN_NAME;
        public const string VERSION = MyPluginInfo.PLUGIN_VERSION;

        internal static ManualLogSource Log;

        internal static SnapshotCamera SnapshotCamera;
        internal static StageComponent CameraStage;

        private void Awake()
        {
            INSTANCE = this;
            Log = Logger;
            try
            {
                if (LobbyCompatibilityChecker.Enabled)
                    LobbyCompatibilityChecker.Init();

                Log.LogInfo("Initializing Configs");

                PluginConfig.Init();

                Log.LogInfo("Preparing SnapshotCamera");

                SnapshotCamera = SnapshotCamera.MakeSnapshotCamera(LayerMask.GetMask("Default", "Player", "Water",
                    "Props", "Room", "InteractableObject", "Foliage", "PhysicsObject", "Enemies", "PlayerRagdoll",
                    "MapHazards", "MiscLevelGeometry", "Terrain"));

                SnapshotCamera.gameObject.hideFlags |= HideFlags.HideAndDontSave;
                DontDestroyOnLoad(SnapshotCamera.gameObject);
                SnapshotCamera.gameObject.transform.position = new Vector3(1000, 1000, 0);

                var stageObject = new GameObject("Stage")
                {
                    transform = { parent = SnapshotCamera.transform }
                };
                CameraStage = stageObject.AddComponent<StageComponent>();

                Log.LogInfo("Patching Methods");

                Harmony.PatchAll();

                Log.LogInfo(NAME + " v" + VERSION + " Loaded!");
            }
            catch (Exception ex)
            {
                Log.LogError("Exception while initializing: \n" + ex);
            }
        }

        internal static class PluginConfig
        {
            internal static ISet<string> Blacklist { get; private set; }
            
            internal static IDictionary<string, Vector3> RotationOverrides { get; private set; }
            internal static IDictionary<string, string> FileOverrides { get; private set; }
            
            private static ConfigEntry<string> _rotationOverridesConfig; 
            private static ConfigEntry<string> _blacklistConfig;
            private static ConfigEntry<string> _fileOverridesConfig;
            
            internal static void Init()
            {
                var config = INSTANCE.Config;
                //Initialize Configs
                _fileOverridesConfig = config.Bind("Overrides", "Manual Files", "",
                    "Dictionary of files to use for specific items");
                
                _blacklistConfig = config.Bind("Config", "BlacklistConfig", "Body,",
                    "List of items to not replace icons");

                _rotationOverridesConfig = config.Bind("Rotations", "Manual Rotation", "Apparatus: -25,45,0|Candy: -75, 0,45|Sticky note: 75,180,10|Toothpaste: -40,-40,45",
                    "Dictionary of alternate rotations for items\nListSeparator=|");
                
                ParseBlacklist();
                _blacklistConfig.SettingChanged += (_, _) => ParseBlacklist();
                
                ParseFileOverrides();
                _fileOverridesConfig.SettingChanged += (_, _) => ParseFileOverrides();
                
                ParseRotationOverrides();
                _rotationOverridesConfig.SettingChanged += (_, _) => ParseRotationOverrides();

                if (LethalConfigProxy.Enabled)
                {
                    LethalConfigProxy.AddConfig(_blacklistConfig);
                    LethalConfigProxy.AddConfig(_fileOverridesConfig);
                    LethalConfigProxy.AddConfig(_rotationOverridesConfig);
                    
                    LethalConfigProxy.AddButton("Debug", "Refresh Held Item", "Regenerate Sprite for held Item", "Refresh",
                        () =>
                        {
                            if (!StartOfRound.Instance)
                                return;
                            
                            if (!StartOfRound.Instance.localPlayerController.currentlyHeldObjectServer)
                                return;
                            
                            GrabbableObjectPatch.ComputeSprite(StartOfRound.Instance.localPlayerController.currentlyHeldObjectServer);
                        });
                }
                
                return;

                void ParseBlacklist()
                {
                    var items = _blacklistConfig.Value.Split(",");

                    Blacklist = items.Select(s => s.Trim()).Where(s => !s.IsNullOrWhiteSpace()).ToHashSet();
                }

                void ParseFileOverrides()
                {
                    var items = _fileOverridesConfig.Value.Split(",");

                    FileOverrides = items.Where(s => !s.IsNullOrWhiteSpace()).Select(s => s.Split(":"))
                        .Where(a => a.Length >= 2).ToDictionary(a => a[0].Trim(), a => a[1].Trim());
                }

                void ParseRotationOverrides()
                {
                    var items = _rotationOverridesConfig.Value.Split("|");

                    RotationOverrides = items.Where(s => !s.IsNullOrWhiteSpace()).Select(s => s.Split(":"))
                        .Where(a => a.Length >= 2).ToDictionary(a => a[0].Trim(), a =>
                        {
                            var tmp = a[1].Trim().Split(",");
                            if (tmp.Length < 3)
                                return Vector3.zero;

                            var parts = new float[3];
                            int i;
                            for (i = 0; i < 3; i++)
                            {
                                if(!float.TryParse(tmp[i], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var result))
                                    break;
                                parts[i] = result;
                            }

                            if (i != 3)
                                return Vector3.zero;
                            
                            return new Vector3(parts[0],parts[1],parts[2]);

                        });
                }
            }


            internal static void CleanAndSave()
            {
                var config = RuntimeIcons.INSTANCE.Config;
                //remove unused options
                var orphanedEntriesProp = AccessTools.Property(config.GetType(), "OrphanedEntries");

                var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp!.GetValue(config, null);

                orphanedEntries.Clear(); // Clear orphaned entries (Unbinded/Abandoned entries)
                config.Save(); // Save the config file
            }
        }
    }
}