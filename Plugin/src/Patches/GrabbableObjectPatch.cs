using System;
using System.IO;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RuntimeIcons.Components;
using RuntimeIcons.Utils;
using UnityEngine;
using Object = System.Object;

namespace RuntimeIcons.Patches;

[HarmonyPatch]
internal class GrabbableObjectPatch
{

    private static Sprite BrokenSprite { get; set; }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
    private static void AfterStart(GrabbableObject __instance)
    {
        if (__instance.itemProperties.itemIcon.name != "ScrapItemIcon" &&
            __instance.itemProperties.itemIcon.name != "ScrapItemIcon2") 
            return;

        if (!BrokenSprite)
        {
            BrokenSprite = UnityEngine.Object.Instantiate(__instance.itemProperties.itemIcon);
            BrokenSprite.name = $"{nameof(RuntimeIcons)}.ScrapItemIcon";
        }
        
        if (PluginConfig.Blacklist.Contains(__instance.itemProperties.itemName))
            return;
            
        ComputeSprite(__instance);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ComputeSprite(GrabbableObject grabbableObject)
    {
        RuntimeIcons.Log.LogWarning($"Computing {grabbableObject.itemProperties.itemName} icon");

        if (PluginConfig.FileOverrides.TryGetValue(grabbableObject.itemProperties.itemName,
                out var filename))
        {
            
            RuntimeIcons.Log.LogWarning($"Assigning {filename} to {grabbableObject.itemProperties.itemName}");
            try
            {
                if (File.Exists(filename))
                {
                    var fileData = File.ReadAllBytes(filename);
                    var texture = new Texture2D(128, 128);
                    texture.LoadImage(fileData);

                    if (texture.width != 128 || texture.height != 128)
                    {
                        UnityEngine.Object.Destroy(texture);
                        RuntimeIcons.Log.LogError($"Expected Icon {filename} has the wrong format!");
                    }
                    else if (!texture.IsTransparent())
                    {
                        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                            new Vector2(texture.width / 2f, texture.height / 2f));
                        sprite.name = $"{nameof(RuntimeIcons)}.{grabbableObject.itemProperties.itemName}";
                        grabbableObject.itemProperties.itemIcon = sprite;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                RuntimeIcons.Log.LogError($"Failed to read {filename}:\n{ex}");
            }
            finally
            {
                RuntimeIcons.Log.LogWarning($"Fallback to Staged image for {grabbableObject.itemProperties.itemName}");
            }
        }

        try
        {
            RuntimeIcons.NewCameraStage.SetItemOnStage(grabbableObject);

            RuntimeIcons.NewCameraStage.FindOptimalRotation();
                
            RuntimeIcons.NewCameraStage.FindOptimalOffsetAndScale();

            var texture = RuntimeIcons.NewCameraStage.TakeSnapshot();

            texture.SavePNG($"{nameof(RuntimeIcons)}.{grabbableObject.itemProperties.itemName}",
                BepInEx.Paths.CachePath);
            
            if (!texture.IsTransparent())
            {
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(texture.width / 2f, texture.height / 2f));
                sprite.name = $"{nameof(RuntimeIcons)}.{grabbableObject.itemProperties.itemName}";
                grabbableObject.itemProperties.itemIcon = sprite;
            }
            else
            {
                RuntimeIcons.Log.LogError($"{grabbableObject.itemProperties.itemName} Generated Empty Sprite!");
                grabbableObject.itemProperties.itemIcon = BrokenSprite;
            }
        }
        finally
        {
            //RuntimeIcons.CameraStage.ResetStage();
            RuntimeIcons.NewCameraStage.ResetStage();
        }
    }
}