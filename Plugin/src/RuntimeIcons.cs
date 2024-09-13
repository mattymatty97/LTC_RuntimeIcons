using System;
using System.Collections.Generic;
using RuntimeIcons.Dependency;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using RuntimeIcons.Components;
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
    }
}