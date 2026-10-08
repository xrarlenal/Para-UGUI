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
            [Tooltip("插在管线的哪一步之后。Transparents 之后能连半透明物体一起糊（UI 通常更想要这个）")]
            public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingTransparents;   // 真实 Overlay 画布下 UI 在这之后才画，所以糊的是「没有 UI 的画面」

            [Tooltip("降采样倍数。0 = 与屏幕同分辨率（默认，质量最高）。只在性能吃紧时才升它，升了就不是全分辨率纹理了")]
            [Range(0, 4)] public int downsample = 0;

            [Tooltip("高斯迭代次数：每次都是横向+纵向一遍，等效 σ 按 √n 变宽")]
            [Range(1, 6)] public int iterations = 2;

            [Tooltip("高斯 σ（以目标像素为单位），就是「核有多宽」")]
            [Range(0.2f, 24f)] public float sigma = 8f;
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
            static readonly int TexelSizeId = Shader.PropertyToID("_FrostedSourceTexelSize");
            static readonly int DirectionId = Shader.PropertyToID("_BlurDirection");
            static readonly int RadiusId = Shader.PropertyToID("_BlurRadius");

            readonly Material _material;
            Settings _settings;

            RTHandle _blurA;
            RTHandle _blurB;

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
                if (_material == null) return;

                var cameraData = renderingData.cameraData;
                var source = cameraData.renderer != null ? cameraData.renderer.cameraColorTargetHandle : null;
                if (source == null) return;

                var descriptor = cameraData.cameraTargetDescriptor;
                int downsample = Mathf.Clamp(_settings.downsample, 0, 4);
                int width = Mathf.Max(1, descriptor.width >> downsample);
                int height = Mathf.Max(1, descriptor.height >> downsample);

                var blurDesc = new RenderTextureDescriptor(width, height, RenderTextureFormat.Default, 0)
                {
                    msaaSamples = 1,
                    useMipMap = false,
                    autoGenerateMips = false,
                    enableRandomWrite = false,
                };

                RenderingUtils.ReAllocateIfNeeded(ref _blurA, blurDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_FrostedBlurA");
                RenderingUtils.ReAllocateIfNeeded(ref _blurB, blurDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_FrostedBlurB");

                var cmd = CommandBufferPool.Get("Frosted Glass Blur");
                cmd.SetGlobalVector(TexelSizeId, new Vector4(1f / width, 1f / height, width, height));
                cmd.SetGlobalFloat(RadiusId, _settings.sigma);

                // 第一遍顺便完成降采样：直接 blit 到 1/2^n 分辨率，双线性就是最省的下采样
                cmd.SetGlobalVector(DirectionId, new Vector2(1f, 0f));
                Blitter.BlitCameraTexture(cmd, source, _blurA, _material, 0);

                for (int i = 0; i < _settings.iterations; i++)
                {
                    cmd.SetGlobalVector(DirectionId, new Vector2(1f, 0f));
                    Blitter.BlitCameraTexture(cmd, _blurA, _blurB, _material, 0);
                    cmd.SetGlobalVector(DirectionId, new Vector2(0f, 1f));
                    Blitter.BlitCameraTexture(cmd, _blurB, _blurA, _material, 0);
                }

                // 公布给 UI：全局纹理这一帧内一直有效
                cmd.SetGlobalTexture(TexId, _blurA.nameID);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public void Dispose()
            {
                _blurA?.Release();
                _blurB?.Release();
                _blurA = null;
                _blurB = null;
            }
        }
    }
}
