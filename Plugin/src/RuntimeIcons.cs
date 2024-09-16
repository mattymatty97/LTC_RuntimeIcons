using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using RuntimeIcons.Components;
using RuntimeIcons.Dependency;
using UnityEngine;

namespace RuntimeIcons
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("VertexLibrary", "0.0.1")]
    [BepInDependency("BMX.LobbyCompatibility", Flags: BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("ainavt.lc.lethalconfig", Flags: BepInDependency.DependencyFlags.SoftDependency)]
    internal class RuntimeIcons : BaseUnityPlugin
    {
        internal static readonly ISet<Hook> Hooks = new HashSet<Hook>();
        internal static readonly Harmony Harmony = new Harmony(GUID);

        public static RuntimeIcons INSTANCE { get; private set; }

        public const string GUID = MyPluginInfo.PLUGIN_GUID;
        public const string NAME = MyPluginInfo.PLUGIN_NAME;
        public const string VERSION = MyPluginInfo.PLUGIN_VERSION;

        internal static ManualLogSource Log;

        //internal static SnapshotCamera SnapshotCamera;
        //internal static StageComponent CameraStage;
        internal static NewStageComponent NewCameraStage;
        
        internal static void VerboseMeshLog(LogType logLevel, Func<string> message)
        {
            LogLevel level;
            switch (logLevel)
            {
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    level = LogLevel.Error;
                    break;
                case LogType.Warning:
                    level = LogLevel.Warning;
                    break;
                default:
                    level = LogLevel.Info;
                    break;
            } 
            VerboseMeshLog(level, message);
        }
    
        internal static void VerboseMeshLog(LogLevel logLevel, Func<string> message)
        {
            Log.Log(logLevel, message());
        }

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
                
                NewCameraStage = NewStageComponent.CreateStage(HideFlags.HideAndDontSave, LayerMask.GetMask("Default", "Player", "Water",
                    "Props", "Room", "InteractableObject", "Foliage", "PhysicsObject", "Enemies", "PlayerRagdoll",
                    "MapHazards", "MiscLevelGeometry", "Terrain"), "Stage");
                DontDestroyOnLoad(NewCameraStage.gameObject);
                NewCameraStage.gameObject.transform.position = new Vector3(0, 1000, 1000);

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