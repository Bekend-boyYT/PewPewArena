using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public sealed class MatrixCodeVisionFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader matrixShader;
    [SerializeField] private LayerMask entityLayerMask = 1 << 8;
    [SerializeField, Range(0.1f, 1f)] private float rainDensity = 0.34f;
    [SerializeField, Min(0.1f)] private float nodeDensity = 7.5f;
    [SerializeField, Min(0f)] private float entityBoost = 1f;

    private static readonly List<MatrixCodeVisionFeature> instances = new List<MatrixCodeVisionFeature>();
    private Material visionMaterial;
    private Material entityMaskMaterial;
    private MatrixCodeVisionPass visionPass;
    private bool visionActive;
    private float revealProgress;
    private float rainSpeed = 1f;

    public static bool SetVision(bool active, float reveal, float speed)
    {
        bool available = false;
        for (int index = instances.Count - 1; index >= 0; index--)
        {
            MatrixCodeVisionFeature feature = instances[index];
            if (feature == null)
            {
                instances.RemoveAt(index);
                continue;
            }

            if (feature.visionMaterial == null || !feature.visionMaterial.shader.isSupported)
            {
                feature.visionActive = false;
                continue;
            }

            feature.visionActive = active;
            feature.revealProgress = Mathf.Clamp01(reveal);
            feature.rainSpeed = Mathf.Max(0.01f, speed);
            available = true;
        }

        return available;
    }

    public override void Create()
    {
        if (!instances.Contains(this)) instances.Add(this);
        if (matrixShader == null) matrixShader = Shader.Find("Hidden/PewPewArena/MatrixCodeVision");
        CoreUtils.Destroy(visionMaterial);
        CoreUtils.Destroy(entityMaskMaterial);
        visionMaterial = matrixShader != null ? CoreUtils.CreateEngineMaterial(matrixShader) : null;
        entityMaskMaterial = matrixShader != null ? CoreUtils.CreateEngineMaterial(matrixShader) : null;
        visionPass = new MatrixCodeVisionPass();
        visionPass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        visionPass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!visionActive || visionMaterial == null || entityMaskMaterial == null || renderingData.cameraData.cameraType != CameraType.Game) return;
        visionPass.Setup(visionMaterial, entityMaskMaterial, entityLayerMask, rainDensity, nodeDensity, entityBoost, rainSpeed, revealProgress);
        renderer.EnqueuePass(visionPass);
    }

    protected override void Dispose(bool disposing)
    {
        instances.Remove(this);
        CoreUtils.Destroy(visionMaterial);
        CoreUtils.Destroy(entityMaskMaterial);
        visionMaterial = null;
        entityMaskMaterial = null;
    }

    private sealed class MatrixCodeVisionPass : ScriptableRenderPass
    {
        private static readonly int SourceColorId = Shader.PropertyToID("_MatrixSourceColor");
        private static readonly int EntityMaskId = Shader.PropertyToID("_MatrixEntityMask");
        private static readonly int RevealId = Shader.PropertyToID("_MatrixReveal");
        private static readonly int RainSpeedId = Shader.PropertyToID("_MatrixRainSpeed");
        private static readonly int DensityId = Shader.PropertyToID("_MatrixDensity");
        private static readonly int NodeDensityId = Shader.PropertyToID("_MatrixNodeDensity");
        private static readonly int EntityBoostId = Shader.PropertyToID("_MatrixEntityBoost");
        private static readonly int FrameId = Shader.PropertyToID("_MatrixFrame");
        private static readonly int HasNormalsId = Shader.PropertyToID("_MatrixHasNormals");
        private static readonly List<ShaderTagId> ShaderTags = new List<ShaderTagId>
        {
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit")
        };

        private Material visionMaterial;
        private Material entityMaskMaterial;
        private LayerMask entityLayerMask;
        private float rainDensity;
        private float nodeDensity;
        private float entityBoost;
        private float rainSpeed;
        private float revealProgress;

        private sealed class EntityPassData
        {
            public RendererListHandle rendererList;
        }

        private sealed class VisionPassData
        {
            public TextureHandle sourceColor;
            public TextureHandle entityMask;
            public TextureHandle depth;
            public TextureHandle normals;
            public TextureHandle destination;
            public Material material;
            public float reveal;
            public float rainSpeed;
            public float density;
            public float nodeDensity;
            public float entityBoost;
            public float frame;
        }

        public void Setup(Material material, Material maskMaterial, LayerMask entityLayers, float density, float nodes, float boost, float speed, float reveal)
        {
            visionMaterial = material;
            entityMaskMaterial = maskMaterial;
            entityLayerMask = entityLayers;
            rainDensity = density;
            nodeDensity = nodes;
            entityBoost = boost;
            rainSpeed = speed;
            revealProgress = reveal;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (visionMaterial == null || entityMaskMaterial == null) return;

            UniversalResourceData resources = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            if (!resources.activeColorTexture.IsValid() || !resources.activeDepthTexture.IsValid()) return;
            TextureHandle depthTexture = resources.cameraDepthTexture.IsValid() ? resources.cameraDepthTexture : resources.activeDepthTexture;
            bool hasNormals = resources.cameraNormalsTexture.IsValid();

            TextureDesc colorDescriptor = renderGraph.GetTextureDesc(resources.activeColorTexture);
            colorDescriptor.name = "Matrix Code Vision Source";
            colorDescriptor.depthBufferBits = DepthBits.None;
            colorDescriptor.msaaSamples = MSAASamples.None;
            colorDescriptor.clearBuffer = false;
            TextureHandle sourceColor = renderGraph.CreateTexture(colorDescriptor);
            renderGraph.AddCopyPass(resources.activeColorTexture, sourceColor, "Copy Matrix Vision Source");

            TextureDesc maskDescriptor = colorDescriptor;
            maskDescriptor.name = "Matrix Entity Mask";
            maskDescriptor.colorFormat = GraphicsFormat.R8_UNorm;
            maskDescriptor.clearBuffer = true;
            maskDescriptor.clearColor = Color.clear;
            TextureHandle entityMask = renderGraph.CreateTexture(maskDescriptor);

            using (var builder = renderGraph.AddRasterRenderPass<EntityPassData>("Render Matrix Entities", out var passData))
            {
                FilteringSettings filtering = new FilteringSettings(RenderQueueRange.all, entityLayerMask);
                DrawingSettings drawing = RenderingUtils.CreateDrawingSettings(ShaderTags, renderingData, cameraData, lightData, cameraData.defaultOpaqueSortFlags);
                drawing.overrideMaterial = entityMaskMaterial;
                drawing.overrideMaterialPassIndex = 1;
                RendererListParams parameters = new RendererListParams(renderingData.cullResults, drawing, filtering);
                passData.rendererList = renderGraph.CreateRendererList(parameters);
                builder.UseRendererList(passData.rendererList);
                builder.SetRenderAttachment(entityMask, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.Read);
                builder.SetRenderFunc(static (EntityPassData data, RasterGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(RTClearFlags.Color, Color.clear, 1f, 0);
                    context.cmd.DrawRendererList(data.rendererList);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<VisionPassData>("World Space Matrix Code Vision", out var passData))
            {
                passData.sourceColor = sourceColor;
                passData.entityMask = entityMask;
                passData.depth = depthTexture;
                passData.normals = resources.cameraNormalsTexture;
                passData.destination = resources.activeColorTexture;
                passData.material = visionMaterial;
                passData.reveal = revealProgress;
                passData.rainSpeed = rainSpeed;
                passData.density = rainDensity;
                passData.nodeDensity = nodeDensity;
                passData.entityBoost = entityBoost;
                passData.frame = Time.unscaledTime;

                builder.UseTexture(sourceColor);
                builder.UseTexture(entityMask);
                builder.UseTexture(passData.depth);
                if (passData.normals.IsValid()) builder.UseTexture(passData.normals);
                builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.Write);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (VisionPassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture(SourceColorId, data.sourceColor);
                    context.cmd.SetGlobalTexture(EntityMaskId, data.entityMask);
                    context.cmd.SetGlobalTexture(Shader.PropertyToID("_CameraDepthTexture"), data.depth);
                    context.cmd.SetGlobalTexture(Shader.PropertyToID("_CameraNormalsTexture"), data.normals);
                    context.cmd.SetGlobalFloat(RevealId, data.reveal);
                    context.cmd.SetGlobalFloat(RainSpeedId, data.rainSpeed);
                    context.cmd.SetGlobalFloat(DensityId, data.density);
                    context.cmd.SetGlobalFloat(NodeDensityId, data.nodeDensity);
                    context.cmd.SetGlobalFloat(EntityBoostId, data.entityBoost);
                    context.cmd.SetGlobalFloat(FrameId, data.frame);
                    context.cmd.SetGlobalFloat(HasNormalsId, data.normals.IsValid() ? 1f : 0f);
                    context.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3, 1);
                });
            }
        }
    }
}
