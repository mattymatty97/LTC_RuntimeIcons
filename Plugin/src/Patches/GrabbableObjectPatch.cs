using HarmonyLib;
using RuntimeIcons.Components;
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
            ComputeSprite(__instance);
        }
    }

    internal static void ComputeSprite(GrabbableObject grabbableObject)
    {
        RuntimeIcons.Log.LogWarning($"Computing {grabbableObject.itemProperties.itemName} icon");

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