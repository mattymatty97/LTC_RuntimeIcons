using CullFactory.Behaviours.API;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RuntimeIcons.Dependency;

internal class CullFactoryCompatibility
{
    public const string GUID = "com.fumiko.CullFactory";

    private static bool? _enabled;

    public static bool Enabled
    {
        get
        {
            _enabled ??= BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GUID);
            return _enabled.Value;
        }
    }

    public static void DisableCullingForCamera(Camera camera)
    {
        if (!Enabled)
            return;

        DisableCullingForCameraImpl(camera);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DisableCullingForCameraImpl(Camera camera)
    {
        var cullingOptions = camera.gameObject.AddComponent<CameraCullingOptions>();
        cullingOptions.disableCulling = true;
    }
}
