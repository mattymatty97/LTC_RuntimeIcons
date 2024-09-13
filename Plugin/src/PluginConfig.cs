using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using RuntimeIcons.Dependency;
using RuntimeIcons.Patches;
using UnityEngine;

namespace RuntimeIcons;

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
        var config = RuntimeIcons.INSTANCE.Config;
        //Initialize Configs
        _fileOverridesConfig = config.Bind("Overrides", "Manual Files", "",
            "Dictionary of files to use for specific items");
                
        _blacklistConfig = config.Bind("Config", "BlacklistConfig", "Body,",
            "List of items to not replace icons");

        _rotationOverridesConfig = config.Bind("Rotations", "Manual Rotation", "Apparatus: -25,45,0|Candy: -43.08,-69.25,75.49|Sticky note: 75,-180,0|Toothpaste: -43.08,-69.25,75.49",
            "Dictionary of alternate rotations for items\nListSeparator=|");
        
        //Sticky note:75,-180,0|Whoopie cushion: -43.08,69.25,-75.49|Candy: -41.64,-62.76,71.12
        
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
                
        CleanAndSave();
        
        RotationEditor.Init();
        
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

internal static class RotationEditor
{
        
    internal static void Init()
    {
        if (LethalConfigProxy.Enabled)
        {
            var configFile = new ConfigFile(Path.GetTempFileName(), false, BepInEx.MetadataHelper.GetMetadata(RuntimeIcons.INSTANCE));

            var eulerAngles = configFile.Bind("Rotation Editor", "Euler Angles", "0,0,0", "The Euler angles representing this rotation");
                
            var angle = configFile.Bind("Rotation Editor", "Angle", 0.0f, new ConfigDescription("rotation angle", new AcceptableValueRange<float>(-360, 360)));
                
            LethalConfigProxy.AddConfig(eulerAngles);
            LethalConfigProxy.AddConfig(angle);
            LethalConfigProxy.AddButton("Rotation Editor", "Apply X Rotation", "translate current EulerAngles around world X Axis by Rotation amount", "X Rot",
                () => ApplyRotation(angle.Value, Vector3.right));
            LethalConfigProxy.AddButton("Rotation Editor", "Apply Y Rotation", "translate current EulerAngles around world X Axis by Rotation amount", "Y Rot",
                () => ApplyRotation(angle.Value, Vector3.up));
            LethalConfigProxy.AddButton("Rotation Editor", "Apply Z Rotation", "translate current EulerAngles around world X Axis by Rotation amount", "Z Rot",
                () => ApplyRotation(angle.Value, Vector3.forward));
                
            return;

            void ApplyRotation(float angle, Vector3 axis)
            {
                var euler = Vector3.zero;
                var arr = eulerAngles.Value.Split(",");
                if (arr.Length == 3)
                {
                    if (float.TryParse(arr[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x))
                    {
                        if (float.TryParse(arr[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                        {
                            if (float.TryParse(arr[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
                            {
                                euler = new Vector3(x, y, z);
                            }
                        }
                    }
                }

                var quat = Quaternion.Euler(euler);
                    
                var rot = Quaternion.AngleAxis(angle, axis);

                var res = rot * quat;

                eulerAngles.Value = $"{WrapAroundAngle(res.eulerAngles.x)},{WrapAroundAngle(res.eulerAngles.y)},{WrapAroundAngle(res.eulerAngles.z)}";
            } 
                
        }
    }

    private static float WrapAroundAngle(float angle)
    {
        // reduce the angle  
        angle %= 360; 

        // force it to be the positive remainder, so that 0 <= angle < 360  
        angle = (angle + 360) % 360;  

        // force into the minimum absolute value residue class, so that -180 < angle <= 180  
        if (angle > 180)  
            angle -= 360;
            
        //round value to not have Exponential Notation
        angle = (float)Math.Round(angle, 2);
            
        return angle;
    }
        
        
}