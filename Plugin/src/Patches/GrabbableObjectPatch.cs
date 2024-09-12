using System;
using System.IO;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RuntimeIcons.Components;
using UnityEngine;
using Object = System.Object;

namespace RuntimeIcons.Patches;

[HarmonyPatch]
internal class GrabbableObjectPatch
{

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
    private static void AfterStart(GrabbableObject __instance)
    {
        if (__instance.itemProperties.itemIcon.name != "ScrapItemIcon2") 
            return;
        
        if (RuntimeIcons.PluginConfig.Blacklist.Contains(__instance.itemProperties.itemName))
            return;
            
        ComputeSprite(__instance);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ComputeSprite(GrabbableObject grabbableObject)
    {
        RuntimeIcons.Log.LogWarning($"Computing {grabbableObject.itemProperties.itemName} icon");

        if (RuntimeIcons.PluginConfig.FileOverrides.TryGetValue(grabbableObject.itemProperties.itemName,
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
                    else
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
            RuntimeIcons.CameraStage.PrepareStageFor(grabbableObject);

            RuntimeIcons.CameraStage.FindOptimalRotationForCamera(RuntimeIcons.SnapshotCamera.cam);
                
            RuntimeIcons.CameraStage.FindOptimalOffsetAndScaleForCamera(RuntimeIcons.SnapshotCamera.cam,
                new Vector2(128, 128));

            var transform = RuntimeIcons.CameraStage.transform;
            var texture = RuntimeIcons.SnapshotCamera.TakeObjectSnapshot(RuntimeIcons.CameraStage.gameObject, 
                transform.localPosition, transform.rotation, transform.localScale);

            SnapshotCamera.SavePNG(texture, $"{nameof(RuntimeIcons)}.{grabbableObject.itemProperties.itemName}",
                BepInEx.Paths.CachePath);

            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(texture.width / 2f, texture.height / 2f));
            sprite.name = $"{nameof(RuntimeIcons)}.{grabbableObject.itemProperties.itemName}";
            grabbableObject.itemProperties.itemIcon = sprite;
        }
        finally
        {
            RuntimeIcons.CameraStage.ResetStage();
        }
    }
}