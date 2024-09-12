using System;
using UnityEngine;
// ReSharper disable MemberCanBePrivate.Global

namespace RuntimeIcons.Components;

public class StageComponent : MonoBehaviour
{
    public GrabbableObject StagedObject { get; private set; }
    public Transform StagedTransform { get; private set; }
    
    public TransformMemory Memory { get; private set; }

    private void Awake()
    {
        transform.localRotation = Quaternion.identity;
        transform.localPosition = Vector3.zero;
    }

    public void PrepareStageFor(GrabbableObject grabbableObject)
    {
        if (StagedObject != null && StagedObject != grabbableObject)
            throw new InvalidOperationException("An Object is already on stage!");
        
        RuntimeIcons.Log.LogInfo($"Setting stage for {grabbableObject.itemProperties.itemName}");
        
        transform.localPosition = Vector3.zero;
        transform.rotation = Quaternion.identity;
        
        StagedObject = grabbableObject;

        var objectToAdjust = grabbableObject.NetworkObject.gameObject;
        
        StagedTransform = objectToAdjust.transform;

        var rotation = Quaternion.Euler(grabbableObject.itemProperties.restingRotation.x, grabbableObject.itemProperties.floorYOffset, grabbableObject.itemProperties.restingRotation.z);
        
        var matrix = Matrix4x4.TRS(Vector3.zero, rotation, StagedTransform.localScale);
        if (!MattyFixes.Utils.VerticesExtensions.TryGetBounds(objectToAdjust, out var bounds, matrix))
            throw new InvalidOperationException("This object has no Renders!");

        Memory = new TransformMemory(StagedTransform);
        
        StagedTransform.SetParent(transform, false);
        StagedTransform.localPosition = -bounds.center;
        StagedTransform.localRotation = rotation;
        
        RuntimeIcons.Log.LogInfo($"Stage Anchor offset {StagedTransform.localPosition} rotation {StagedTransform.localRotation.eulerAngles}");
    }

    public void FindOptimalRotationForCamera(Camera camera)
    {
        transform.Rotate(camera.transform.up, -45, Space.World);
        transform.Rotate(camera.transform.right, -25, Space.World);
        RuntimeIcons.Log.LogInfo($"Stage rotation {transform.localRotation.eulerAngles}");
    }

    public void FindOptimalOffsetAndScaleForCamera(Camera camera, Vector2 targetPixelArea)
    {
        var matrix = Matrix4x4.TRS(Vector3.zero, transform.rotation, Vector3.one);
        if (!MattyFixes.Utils.VerticesExtensions.TryGetBounds(gameObject, out var bounds, matrix))
            throw new InvalidOperationException("This object has no Renders!");
        
        if (bounds.size == Vector3.zero)
            throw new InvalidOperationException("This object has no Bounds!");
        
        // Calculate the scale factor needed to fit within the target area
        var pixelsPerUnit = targetPixelArea.y / (2f * camera.orthographicSize);
        var targetWorldArea = targetPixelArea / pixelsPerUnit;

        // Calculate the scale factor considering the object's distance from the camera
        // also add some padding from the sides
        var scaleFactorX = (targetWorldArea.x - 0.2f) / bounds.size.x;
        var scaleFactorY = (targetWorldArea.y - 0.2f) / bounds.size.y;
        var scaleFactor = Mathf.Min(scaleFactorX, scaleFactorY);
        
        // Apply the calculated scale factor
        var scale = Vector3.one * scaleFactor;
        
        // Calculate the new bounds size after applying scale
        var scaledSize = Vector3.Scale(bounds.size, scale);
        var scaledCenter = Vector3.Scale(bounds.center, scale);

        // Calculate the desired Z position to prevent clipping
        var distanceToMove = scaledSize.z + camera.nearClipPlane;

        // Calculate the offset in the camera's local space
        var localOffset = new Vector3(-scaledCenter.x, -scaledCenter.y, distanceToMove);
        
        RuntimeIcons.Log.LogInfo($"Stage offset {localOffset} scale {scale}");

        transform.localPosition = localOffset;
        transform.localScale = scale;
    }

    public void ResetStage()
    {
        if (StagedObject != null)
        {
            StagedTransform.SetParent(Memory.Parent, false);

            StagedTransform.localPosition = Memory.LocalPosition;
            StagedTransform.localRotation = Memory.LocalRotation;
            StagedTransform.localScale = Memory.LocalScale;
        }

        StagedObject = null;
        StagedTransform = null;
        Memory = default;
        
        transform.localPosition = Vector3.zero;
        transform.rotation = Quaternion.identity;
    }
    
    public record struct TransformMemory
    {
        public readonly Transform Parent;
        public readonly Vector3 LocalPosition;
        public readonly Quaternion LocalRotation;
        public readonly Vector3 LocalScale;

        public TransformMemory(Transform target)
        {
            this.Parent = target.parent;
            this.LocalPosition = target.localPosition;
            this.LocalRotation = target.localRotation;
            this.LocalScale = target.localScale;
        }
    }

}