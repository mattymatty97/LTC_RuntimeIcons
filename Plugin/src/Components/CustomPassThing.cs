using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.RendererUtils;

public class CustomPassThing : CustomPass
{
    public RenderTexture targetTexture;
    public SortingCriteria sortingCriteria = SortingCriteria.SortingLayer | SortingCriteria.RenderQueue | SortingCriteria.OptimizeStateChanges | SortingCriteria.CanvasOrder;

    private static ShaderTagId[] shaderTags;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        shaderTags = new ShaderTagId[]
        {
            new(HDShaderPassNames.s_ShadowCasterStr),
            HDShaderPassNames.s_DepthOnlyName,
            HDShaderPassNames.s_TransparentDepthPrepassName,
            HDShaderPassNames.s_TransparentBackfaceName,
            HDShaderPassNames.s_ForwardName,
            HDShaderPassNames.s_ForwardOnlyName,
            HDShaderPassNames.s_SRPDefaultUnlitName,
            HDShaderPassNames.s_DecalMeshForwardEmissiveName,
            HDShaderPassNames.s_TransparentDepthPostpassName,
        };
    }

    private void Render(ref CustomPassContext ctx, in RenderQueueRange range, PerObjectData configuration, bool fptl)
    {
        var camera = ctx.hdCamera.camera;
        RendererList rendererList = ctx.renderContext.CreateRendererList(new RendererListDesc(shaderTags, ctx.cullingResults, camera)
        {
            renderQueueRange = range,
            rendererConfiguration = configuration,
            sortingCriteria = sortingCriteria,
            excludeObjectMotionVectors = false,
            layerMask = camera.cullingMask,
        });
        CoreUtils.SetKeyword(ctx.cmd, "USE_FPTL_LIGHTLIST", fptl);
        CoreUtils.SetKeyword(ctx.cmd, "USE_CLUSTERED_LIGHTLIST", !fptl);
        CoreUtils.DrawRendererList(ctx.renderContext, ctx.cmd, rendererList);
    }

    protected override void Execute(CustomPassContext ctx)
    {
        ctx.cmd.SetRenderTarget(targetTexture.colorBuffer, targetTexture.depthBuffer);
        ctx.cmd.ClearRenderTarget(true, true, Color.clear);

        var frameSettings = ctx.hdCamera.frameSettings;
        PerObjectData rendererConfiguration = HDUtils.GetRendererConfiguration(frameSettings.IsEnabled(FrameSettingsField.ProbeVolume), frameSettings.IsEnabled(FrameSettingsField.Shadowmask));

        Render(ref ctx, RenderQueueRange.opaque, rendererConfiguration, frameSettings.IsEnabled(FrameSettingsField.FPTLForForwardOpaque));

        Render(ref ctx, RenderQueueRange.transparent, rendererConfiguration, false);
    }
}
