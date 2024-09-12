using System;
using System.Linq;
using HarmonyLib;
using RuntimeIcons.Components;
using RuntimeIcons.Utils;
using UnityEngine;

namespace RuntimeIcons.Patches;

[HarmonyPatch]
internal class GrabbableObjectPatch
{

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
    private static void AfterStart(GrabbableObject __instance)
    {
        if (__instance.itemProperties.itemIcon.name == "ScrapItemIcon2")
        {
            RuntimeIcons.Log.LogInfo($"Computing {__instance.itemProperties.itemName} icon");

            var rotation = Quaternion.Euler(__instance.itemProperties.restingRotation.x, __instance.itemProperties.floorYOffset, __instance.itemProperties.restingRotation.z) * Quaternion.Euler(-25, 45, 0);
            //var rotation = Quaternion.Euler(__instance.itemProperties.rotationOffset);
            
            (float scaleFactor, Vector3 offset) =
                ObjectAlignmentUtils.CalculateAdjustmentsForCameraArea(__instance, rotation, RuntimeIcons.SnapshotCamera.cam,
                    new Vector2(128, 128));
            
            var scale = __instance.NetworkObject.transform.localScale * scaleFactor;
            
            RuntimeIcons.Log.LogWarning($"{offset} - {scale}");
            
            var texture = RuntimeIcons.SnapshotCamera.TakeObjectSnapshot(__instance.NetworkObject.gameObject,
                offset, rotation , scale);

            SnapshotCamera.SavePNG(texture, $"{nameof(RuntimeIcons)}.{__instance.itemProperties.itemName}",
                BepInEx.Paths.CachePath);

            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(texture.width / 2f, texture.height / 2f));
            sprite.name = $"{nameof(RuntimeIcons)}.{__instance.itemProperties.itemName}";
            __instance.itemProperties.itemIcon = sprite;
        }
    }
    
    
}