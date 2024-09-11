using System.Linq;
using UnityEngine;

namespace RuntimeIcons.Utils;

public static class ObjectAlignmentUtils
{
    
    public static (float scaleFactor, Vector3 offset) CalculateAdjustmentsForCameraArea(GrabbableObject grabbable, Quaternion rotation, Camera camera, Vector2 targetPixelArea)
    {
        if (grabbable == null || camera == null) return (1f, Vector3.zero);

        var objectToAdjust = grabbable.NetworkObject.gameObject;
        
        // Get the bounds of the object
        var matrix = Matrix4x4.TRS(Vector3.zero, rotation, objectToAdjust.transform.localScale);
        if (!MattyFixes.Utils.VerticesExtensions.TryGetBounds(objectToAdjust, out var originalBounds, matrix))
            return (1f, Vector3.zero);
        
        if (originalBounds.size == Vector3.zero) return (1f, Vector3.zero); 

        // Calculate the scale factor needed to fit within the target area
        float pixelsPerUnit = targetPixelArea.y / (2f * camera.orthographicSize);  // Use RenderTexture height for pixel per unit
        Vector2 targetWorldArea = targetPixelArea / pixelsPerUnit;

        // Calculate the scale factor considering the object's distance from the camera
        // also add some padding from the sides
        float scaleFactorX = (targetWorldArea.x - 0.2f) / originalBounds.size.x;
        float scaleFactorY = (targetWorldArea.y - 0.2f) / originalBounds.size.y;
        float scaleFactor = Mathf.Min(scaleFactorX, scaleFactorY);
        
        // Apply the calculated scale factor
        Vector3 scale = Vector3.one * scaleFactor;

        // Calculate the offset needed to avoid clipping and center the object
        Vector3 offset = CalculateOffsetToPreventClipping(originalBounds, camera, scale);

        return (scaleFactor, offset);
    }

    
    private static Vector3 CalculateOffsetToPreventClipping(Bounds bounds, Camera camera, Vector3 scale)
    {
        // Calculate the new bounds size after applying scale
        Vector3 scaledSize = Vector3.Scale(bounds.size, scale);
        Vector3 scaledCenter = Vector3.Scale(bounds.center, scale);

        // Calculate the desired Z position to prevent clipping
        float distanceToMove = scaledSize.z + camera.nearClipPlane;

        // Calculate the offset in the camera's local space
        Vector3 localOffset = new Vector3(-scaledCenter.x, -scaledCenter.y, distanceToMove);

        return localOffset;
    }
    
}