using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using RuntimeIcons.Components;
using RuntimeIcons.Dependency;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using LogType = VertexLibrary.LogType;

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

        internal static StageComponent CameraStage;

        internal static void VerboseMeshLog(LogType logLevel, Func<string> message)
        {
            var level = logLevel switch
            {
                LogType.Fatal => LogLevel.Fatal,
                LogType.Error => LogLevel.Error,
                LogType.Warning => LogLevel.Warning,
                LogType.Info1 or LogType.Info2 or LogType.Info3 or LogType.Info4 or LogType.Info => LogLevel.Info,
                LogType.Debug1 or LogType.Debug2 or LogType.Debug3 or LogType.Debug4 or LogType.Debug => LogLevel.Debug,
                LogType.All => LogLevel.All,
                _ => LogLevel.None,
            };
                
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

                Log.LogInfo("Preparing Stage");
                
                SetStage();

                Log.LogInfo("Patching Methods");

                Harmony.PatchAll();

                Log.LogInfo(NAME + " v" + VERSION + " Loaded!");
            }
            catch (Exception ex)
            {
                Log.LogError("Exception while initializing: \n" + ex);
            }
        }

        private void SetStage()
        {

                CameraStage = StageComponent.CreateStage(HideFlags.HideAndDontSave, LayerMask.GetMask("Default",
                    "Player", "Water",
                    "Props", "Room", "InteractableObject", "Foliage", "PhysicsObject", "Enemies", "PlayerRagdoll",
                    "MapHazards", "MiscLevelGeometry", "Terrain"), $"{nameof(RuntimeIcons)}.Stage", false);
                DontDestroyOnLoad(CameraStage.gameObject);
                CameraStage.gameObject.transform.position = new Vector3(0, 1000, 1000);

                //add ceiling light!
                var lightGo1 = new GameObject("SpotLight 1")
                {
                    hideFlags = hideFlags,
                    layer = 1,
                    transform =
                    {
                        parent = CameraStage.LightTransform,
                        localPosition = new Vector3(0, 3, 0),
                        rotation = Quaternion.LookRotation(Vector3.down)
                    }
                };

                var light = lightGo1.AddComponent<Light>();
                light.type = LightType.Spot;
                light.shape = LightShape.Cone;
                light.color = Color.white;
                light.colorTemperature = 6901;
                light.useColorTemperature = true;
                light.shadows = LightShadows.Soft;
                light.spotAngle = 50.0f;
                light.innerSpotAngle = 21.8f;
                light.range = 7.11f;

                var lightData = lightGo1.AddComponent<HDAdditionalLightData>();
                lightData.affectDiffuse = true;
                lightData.affectSpecular = true;
                lightData.affectsVolumetric = true;
                lightData.applyRangeAttenuation = true;
                lightData.color = Color.white;
                lightData.colorShadow = true;
                lightData.shadowDimmer = 0.8f;
                lightData.customSpotLightShadowCone = 30f;
                lightData.distance = 150000000000;
                lightData.fadeDistance = 10000;
                lightData.innerSpotPercent = 82.7f;
                lightData.intensity = 75f;

                // add front light ( similar to ceiling one but facing a 45 angle )
                var lightGo2 = new GameObject("SpotLight 2")
                {
                    hideFlags = hideFlags,
                    layer = 1,
                    transform =
                    {
                        parent = CameraStage.LightTransform,
                        localPosition = new Vector3(-2.7f, 0, -2.7f),
                        rotation = Quaternion.Euler(0, 45, 0)
                    }
                };

                var light2 = lightGo2.AddComponent<Light>();
                light2.type = LightType.Spot;
                light2.shape = LightShape.Cone;
                light2.color = Color.white;
                light2.colorTemperature = 6901;
                light2.useColorTemperature = true;
                light2.shadows = LightShadows.Soft;
                light2.spotAngle = 50.0f;
                light2.innerSpotAngle = 21.8f;
                light2.range = 7.11f;

                var lightData2 = lightGo2.AddComponent<HDAdditionalLightData>();
                lightData2.affectDiffuse = true;
                lightData2.affectSpecular = true;
                lightData2.affectsVolumetric = true;
                lightData2.applyRangeAttenuation = true;
                lightData2.color = Color.white;
                lightData2.colorShadow = true;
                lightData2.shadowDimmer = 0.6f;
                lightData2.customSpotLightShadowCone = 30f;
                lightData2.distance = 150000000000;
                lightData2.fadeDistance = 10000;
                lightData2.innerSpotPercent = 82.7f;
                lightData2.intensity = 50f;
                lightData2.shapeRadius = 0.5f;

                // add a second front light ( similar to the other one but does not have Specular )
                var lightGo3 = new GameObject("SpotLight 3")
                {
                    hideFlags = hideFlags,
                    layer = 1,
                    transform =
                    {
                        parent = CameraStage.LightTransform,
                        localPosition = new Vector3(2.7f, 0, -2.7f),
                        rotation = Quaternion.Euler(0, -45, 0)
                    }
                };

                var light3 = lightGo3.AddComponent<Light>();
                light3.type = LightType.Spot;
                light3.shape = LightShape.Cone;
                light3.color = Color.white;
                light3.colorTemperature = 6901;
                light3.useColorTemperature = true;
                light3.shadows = LightShadows.Soft;
                light3.spotAngle = 50.0f;
                light3.innerSpotAngle = 21.8f;
                light3.range = 7.11f;

                var lightData3 = lightGo3.AddComponent<HDAdditionalLightData>();
                lightData3.affectDiffuse = true;
                lightData3.affectSpecular = false;
                lightData3.affectsVolumetric = true;
                lightData3.applyRangeAttenuation = true;
                lightData3.color = Color.white;
                lightData3.colorShadow = true;
                lightData3.shadowDimmer = 0.4f;
                lightData3.customSpotLightShadowCone = 30f;
                lightData3.distance = 150000000000;
                lightData3.fadeDistance = 10000;
                lightData3.innerSpotPercent = 82.7f;
                lightData3.intensity = 30f;
        }
    }
}