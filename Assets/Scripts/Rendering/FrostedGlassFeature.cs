using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GlassUI
{
    /// <summary>
    /// 毛玻璃的核心：在 URP 管线里插一个 Pass —— 把当前相机颜色拷出来 → 降采样 → 横竖分离模糊 →
    /// 作为全局纹理 _FrostedGlassTex 公布出去。
    ///
    /// 之后 UI 那边的 UI/FrostedGlass 着色器按屏幕 UV 采这张图，
    /// 每块面板就“恰好”显示它身后那一块被糊掉的画面。
    ///
    /// 挂在 ScriptableRendererData 上（Settings/ 下的 Renderer 资产），不是挂在物件上。
    /// </summary>
    [DisallowMultipleRendererFeature("Frosted Glass Blur")]
    public class FrostedGlassFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            [Tooltip("插在管线的哪一步之后。放在 Transparents 之后才能把半透明物体也糊进去（真实 Overlay 画布下 UI 还没画）")]
            public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingTransparents;
        }

        public Settings settings = new Settings();

        FrostedGlassPass _pass;
        Material _material;

        public override void Create()
        {
            var shader = Shader.Find("Hidden/FrostedGlass/Blur");
            if (shader == null)
            {
                Debug.LogError("[FrostedGlass] 找不到 Shader \"Hidden/FrostedGlass/Blur\"");
                return;
            }

            _material = CoreUtils.CreateEngineMaterial(shader);
            _material.hideFlags = HideFlags.HideAndDontSave;
            _pass = new FrostedGlassPass(_material) { renderPassEvent = settings.injectionPoint };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_pass == null || _material == null) return;

            var cameraType = renderingData.cameraData.cameraType;
            if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection) return;

            _pass.Configure(settings);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            _pass?.Dispose();
            CoreUtils.Destroy(_material);
        }

        // ------------------------------------------------------------------

        class FrostedGlassPass : ScriptableRenderPass
        {
            /// <summary>UI 着色器里用的全局纹理名。</summary>
            public const string GlobalTexture = "_FrostedGlassTex";

            static readonly int TexId = Shader.PropertyToID(GlobalTexture);

            readonly Material _material;
            Settings _settings;

            RTHandle _blurA;

            public FrostedGlassPass(Material material)
            {
                _material = material;
                _settings = new Settings();
            }

            public void Configure(Settings settings)
            {
                _settings = settings;
                renderPassEvent = settings.injectionPoint;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (_blurA == null && !Allocate(renderingData)) return;

                var cameraData = renderingData.cameraData;
                var source = cameraData.renderer != null ? cameraData.renderer.cameraColorTargetHandle : null;
                if (source == null) return;

                var cmd = CommandBufferPool.Get("Frosted Glass Mips");
                // 1) 拷「没有 UI 的画面」进 mip0
                Blitter.BlitCameraTexture(cmd, source, _blurA);
                // 2) 生成整条 mip 链：每一级就是上一级缩小一半 = 逐级更糊
                cmd.GenerateMips(_blurA.nameID);
                // 3) 公布给 UI，UI 用 textureLod 自己决定取哪一级
                cmd.SetGlobalTexture(TexId, _blurA.nameID);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            bool Allocate(in RenderingData renderingData)
            {
                var descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.msaaSamples = 1;
                descriptor.depthBufferBits = 0;
                descriptor.useMipMap = true;          // 关键：要 mip 链
                descriptor.autoGenerateMips = false;  // 自己生成，避免多余开销
                RenderingUtils.ReAllocateIfNeeded(ref _blurA, descriptor, FilterMode.Trilinear,
                    TextureWrapMode.Clamp, name: "_FrostedGlassMip");
                return _blurA != null;
            }

            public void Dispose()
            {
                _blurA?.Release();
                _blurA = null;
            }
        }
    }
}
