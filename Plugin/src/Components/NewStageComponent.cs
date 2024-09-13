using System;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.Rendering.HighDefinition;

namespace RuntimeIcons.Components;

public class NewStageComponent : MonoBehaviour
{

    private NewStageComponent(){}

    private GameObject LightGo { get; set; }
    private GameObject TargetGo { get; set; }
    private GameObject CameraGo { get; set; }

    private Transform LightTransform => LightGo.transform;
    private Transform TargetTransform => TargetGo.transform;
    private Transform CameraTransform => CameraGo.transform;
    
    private Camera _camera;
    private HDAdditionalCameraData _cameraSettings;
    private CustomPassThing _cameraPass;
    
    
    public GrabbableObject StagedObject { get; private set; }
    public Transform StagedTransform { get; private set; }
    
    public TransformMemory Memory { get; private set; }

    public static NewStageComponent CreateStage(HideFlags hideFlags, int layerMask = 1,string stageName = "Stage")
    {
        
        //create the root Object for the Stage
        var stageGo = new GameObject(stageName)
        {
            hideFlags = hideFlags
        };

        //add the component to the stage
        var stageComponent = stageGo.AddComponent<NewStageComponent>();
        
        //add the StageTarget
        var targetGo = new GameObject("Target")
        {
            hideFlags = hideFlags,
            transform =
            {
                parent = stageGo.transform
            }
        };
        stageComponent.TargetGo = targetGo;

        //add the stage Lights
        
        var lightsGo = new GameObject("Stage Lights")
        {
            hideFlags = hideFlags,
            transform =
            {
                parent = stageGo.transform
            }
        };
        //disable the lights by default
        lightsGo.SetActive(false);
        stageComponent.LightGo = lightsGo;
        
        //add ceiling light!
        var lightGo1 = new GameObject("SpotLight 1")
        {
            hideFlags = hideFlags,
            transform =
            {
                parent = lightsGo.transform,
                localPosition = new Vector3(0, 4, 0),
                rotation = Quaternion.LookRotation(Vector3.down)
            }
        };

        var light = lightGo1.AddComponent<Light>();
        light.type = LightType.Spot;
        light.shape = LightShape.Cone;
        light.color = Color.white;
        light.colorTemperature = 6901;
        light.useColorTemperature = true;
        light.shadows = LightShadows.None;
        light.intensity = 250f;
        light.spotAngle = 160.0f;
        light.innerSpotAngle = 21.8f;
        light.range = 7.11f;
        
        var lightData = lightGo1.AddComponent<HDAdditionalLightData>();
        lightData.affectDiffuse = true;
        lightData.affectSpecular = true;
        lightData.affectsVolumetric = true;
        lightData.applyRangeAttenuation = true;
        lightData.color = Color.white;
        lightData.colorShadow = true;
        lightData.customSpotLightShadowCone = 30f;
        lightData.distance = 150000000000;
        lightData.fadeDistance = 10000;
        lightData.innerSpotPercent = 82.7f;
        lightData.intensity = 1000f;
        
        // add front light ( clone the ceiling one but rotate it )
        var lightGo2 = Instantiate(lightGo1, lightsGo.transform);
        lightGo2.name = "SpotLight 2";
        lightGo2.hideFlags = hideFlags;
        lightGo2.transform.localPosition = new Vector3(0, 0, -4);
        lightGo2.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        
        //add Camera
        var cameraGo = new GameObject("Camera")
        {
            hideFlags = hideFlags,
            transform =
            {
                parent = stageGo.transform
            }
        };
        stageComponent.CameraGo = cameraGo;

        // Add a Camera component to the GameObject
        var cam = cameraGo.AddComponent<Camera>();
        stageComponent._camera = cam;

        // Configure the Camera
        cam.cullingMask = layerMask;
        cam.orthographic = true;
        cam.orthographicSize = 1;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.clear;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 10f;
        cam.enabled = false;
        
        // Add a Camera component to the GameObject
        HDAdditionalCameraData camData = cameraGo.AddComponent<HDAdditionalCameraData>();
        stageComponent._cameraSettings = camData;

        camData.clearDepth = true;
        camData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
        camData.backgroundColorHDR = Color.clear;
        camData.customRenderingSettings = true;
        camData.customRenderingSettings = true;
        var overrideMask = camData.renderingPathCustomFrameSettingsOverrideMask;
        overrideMask.mask[(uint)FrameSettingsField.DecalLayers] = true;
        camData.renderingPathCustomFrameSettingsOverrideMask = overrideMask;
        camData.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.DecalLayers, false);

        var customPassVolume = cameraGo.AddComponent<CustomPassVolume>();
        customPassVolume.targetCamera = cam;
        
        var customPass = (CustomPassThing)customPassVolume.AddPassOfType<CustomPassThing>();
        stageComponent._cameraPass = customPass;
        
        customPass.targetColorBuffer = CustomPass.TargetBuffer.Custom;
        customPass.targetDepthBuffer = CustomPass.TargetBuffer.Custom;
        customPass.clearFlags = UnityEngine.Rendering.ClearFlag.All;
        
        return stageComponent;
    }

    public void SetItemOnStage(GrabbableObject grabbableObject)
    {
        if (StagedObject != null && StagedObject != grabbableObject)
            throw new InvalidOperationException("An Object is already on stage!");
        
        RuntimeIcons.Log.LogInfo($"Setting stage for {grabbableObject.itemProperties.itemName}");
        
        TargetTransform.localPosition = Vector3.zero;
        TargetTransform.rotation = Quaternion.identity;
        
        StagedObject = grabbableObject;

        var objectToAdjust = grabbableObject.NetworkObject.gameObject;
        
        StagedTransform = objectToAdjust.transform;

        var rotation = Quaternion.Euler(grabbableObject.itemProperties.restingRotation.x, grabbableObject.itemProperties.floorYOffset, grabbableObject.itemProperties.restingRotation.z);
        //var rotation = Quaternion.Euler(grabbableObject.itemProperties.rotationOffset.x, grabbableObject.itemProperties.rotationOffset.y, grabbableObject.itemProperties.rotationOffset.z);
        
        var matrix = Matrix4x4.TRS(Vector3.zero, rotation, StagedTransform.localScale);
        if (!MattyFixes.Utils.VerticesExtensions.TryGetBounds(objectToAdjust, out var bounds, matrix))
            throw new InvalidOperationException("This object has no Renders!");

        Memory = new TransformMemory(StagedTransform);
        
        StagedTransform.SetParent(TargetTransform, false);
        StagedTransform.localPosition = -bounds.center;
        StagedTransform.localRotation = rotation;
        
        RuntimeIcons.Log.LogInfo($"Stage Anchor offset {StagedTransform.localPosition} rotation {StagedTransform.localRotation.eulerAngles}");
    }

    public void SetObjectOnStage(GameObject gameObject)
    {
        SetObjectOnStage(gameObject.transform);
    }
    
    public void SetObjectOnStage(Transform transform)
    {
        if (StagedTransform != null || StagedTransform != transform)
            throw new InvalidOperationException("An Object is already on stage!");
        
        RuntimeIcons.Log.LogInfo($"Setting stage for {transform.name}");
        
        TargetTransform.localPosition = Vector3.zero;
        TargetTransform.rotation = Quaternion.identity;
        
        StagedTransform = transform;
        
        var matrix = Matrix4x4.TRS(Vector3.zero, transform.rotation, transform.localScale);
        if (!MattyFixes.Utils.VerticesExtensions.TryGetBounds(transform.gameObject, out var bounds, matrix))
            throw new InvalidOperationException("This object has no Renders!");

        Memory = new TransformMemory(StagedTransform);
        
        StagedTransform.SetParent(TargetTransform, false);
        StagedTransform.localPosition = -bounds.center;
        
        RuntimeIcons.Log.LogInfo($"Stage Anchor offset {StagedTransform.localPosition} rotation {StagedTransform.localRotation.eulerAngles}");
    }

    public void FindOptimalRotation()
    {
        if (StagedObject == null)
            throw new InvalidOperationException("No Object on stage!");
        

        if (PluginConfig.RotationOverrides.TryGetValue(StagedObject.itemProperties.itemName,
                out var rotations))
        {
            TargetTransform.Rotate(rotations, Space.World);
        }
        else
        {
            //if (StagedObject.itemProperties.twoHandedAnimation)
            //    TargetTransform.Rotate(_camera.transform.up, -135, Space.World);

            var matrix = Matrix4x4.TRS(Vector3.zero, TargetTransform.rotation, Vector3.one);
            if (!MattyFixes.Utils.VerticesExtensions.TryGetBounds(TargetGo, out var bounds, matrix))
                throw new InvalidOperationException("This object has no Renders!");

            if (bounds.size == Vector3.zero)
                throw new InvalidOperationException("This object has no Bounds!");

            if (bounds.extents.y < bounds.extents.x / 2f && bounds.extents.y <  bounds.extents.z / 2f)
            {
                RuntimeIcons.Log.LogDebug($"{StagedObject.itemProperties.itemName} rotated -75 x");
                TargetTransform.Rotate(_camera.transform.right, -75, Space.World);

                if (bounds.extents.z < bounds.extents.x)
                {
                    RuntimeIcons.Log.LogDebug($"{StagedObject.itemProperties.itemName} rotated -45 z");
                    TargetTransform.Rotate(_camera.transform.forward, -45, Space.World);
                }
                else if (bounds.extents.x < bounds.extents.z * 0.85f)
                {
                    RuntimeIcons.Log.LogDebug($"{StagedObject.itemProperties.itemName} rotated -45 z");
                    TargetTransform.Rotate(_camera.transform.forward, -45, Space.World);
                }
            }
            else
            {
                RuntimeIcons.Log.LogDebug($"{StagedObject.itemProperties.itemName} rotated -25 x");
                TargetTransform.Rotate(_camera.transform.right, -25, Space.World);

                if (bounds.extents.x < bounds.extents.z * 0.85f)
                {
                    RuntimeIcons.Log.LogDebug($"{StagedObject.itemProperties.itemName} rotated -45 y");
                    TargetTransform.Rotate(_camera.transform.up, -45, Space.World);
                }
                else if (bounds.extents.y < bounds.extents.x / 2f || bounds.extents.x < bounds.extents.y / 2f)
                {
                    RuntimeIcons.Log.LogDebug($"{StagedObject.itemProperties.itemName} rotated 45 z");
                    TargetTransform.Rotate(_camera.transform.forward, 45, Space.World);
                }
            }
        }

        RuntimeIcons.Log.LogInfo($"Stage rotation {TargetTransform.localRotation.eulerAngles}");
    }

    public void FindOptimalOffsetAndScale()
    {
        FindOptimalOffsetAndScale(new Vector2(128, 128));
    }

    public void FindOptimalOffsetAndScale(Vector2 targetPixelArea)
    {
        if (StagedTransform == null)
            throw new InvalidOperationException("No Object on stage!");
        
        var matrix = Matrix4x4.TRS(Vector3.zero, TargetTransform.rotation, Vector3.one);
        if (!MattyFixes.Utils.VerticesExtensions.TryGetBounds(TargetGo, out var bounds, matrix))
            throw new InvalidOperationException("This object has no Renders!");
        
        if (bounds.size == Vector3.zero)
            throw new InvalidOperationException("This object has no Bounds!");
        
        // Calculate the scale factor needed to fit within the target area
        var pixelsPerUnit = targetPixelArea.y / (2f * _camera.orthographicSize);
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
        var distanceToMove = Mathf.Max(scaledSize.z + _camera.nearClipPlane, 1f);

        // Calculate the offset in the camera's local space
        var localOffset = new Vector3(-scaledCenter.x, -scaledCenter.y, distanceToMove);
        
        RuntimeIcons.Log.LogInfo($"Stage offset {localOffset} scale {scale}");

        TargetTransform.localPosition = localOffset;
        TargetTransform.localScale = scale;
    }

    public void ResetStage()
    {
        if (StagedTransform != null)
        {
            StagedTransform.SetParent(Memory.Parent, false);

            StagedTransform.localPosition = Memory.LocalPosition;
            StagedTransform.localRotation = Memory.LocalRotation;
            StagedTransform.localScale = Memory.LocalScale;
        }

        StagedObject = null;
        StagedTransform = null;
        Memory = default;
        
        TargetTransform.localPosition = Vector3.zero;
        TargetTransform.rotation = Quaternion.identity;
    }

    public Texture2D TakeSnapshot(int width = 128, int height = 128)
    {
        return TakeSnapshot(Color.clear, width, height);
    }
    
    public Texture2D TakeSnapshot(Color backgroundColor, int width = 128, int height = 128)
    {
        //Turn on the stage Lights
        LightGo.SetActive(true);
        
        // Set the background color of the camera
        _camera.backgroundColor = backgroundColor;

        // Get a temporary render texture and render the camera
        var tempTexture = RenderTexture.GetTemporary(width, height, 8, RenderTextureFormat.ARGB32);
        var tempTexture2 = RenderTexture.GetTemporary(width, height, 8, RenderTextureFormat.ARGB32);
        _camera.targetTexture = tempTexture2;
        _cameraPass.targetTexture = tempTexture;
        _camera.Render();
        
        //Turn off the stage Lights
        LightGo.SetActive(false);
        
        // Activate the temporary render texture
        RenderTexture previouslyActiveRenderTexture = RenderTexture.active;
        RenderTexture.active = tempTexture;
        
        // Extract the image into a new texture without mipmaps
        Texture2D texture = new Texture2D(tempTexture.width, tempTexture.height, TextureFormat.ARGB32,  -1,false);
        
        texture.ReadPixels(new Rect(0, 0, tempTexture.width, tempTexture.height), 0, 0);
        texture.Apply();
        
        // Reactivate the previously active render texture
        RenderTexture.active = previouslyActiveRenderTexture;
        
        // Clean up after ourselves
        _camera.targetTexture = null;
        RenderTexture.ReleaseTemporary(tempTexture2);
        _cameraPass.targetTexture = null;
        RenderTexture.ReleaseTemporary(tempTexture);
        
        // Return the texture
        return texture;
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