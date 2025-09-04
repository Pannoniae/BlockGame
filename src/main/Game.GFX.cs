using System.Numerics;
using System.Runtime.InteropServices;
using BlockGame.ui;
using BlockGame.util;
using Silk.NET.Maths;
using Silk.NET.OpenGL.Legacy;
using Silk.NET.OpenGL.Legacy.Extensions.ARB;
using Silk.NET.OpenGL.Legacy.Extensions.NV;
using Silk.NET.OpenGL.Legacy.Extensions.EXT;
using Silk.NET.Windowing;

namespace BlockGame;

public partial class Game {
    public uint fbo;
    public uint FBOtex;
    public uint throwawayVAO;
    public uint depthBuffer;

    public static bool sampleShadingSupported = false;
    private static bool sampleShadingEnabled = false;

    public static bool hasSBL = false;
    public static bool hasVBUM = false;
    public static bool hasUBUM = false;
    public static bool hasInstancedUBO = false;
    public static bool hasCMDL = false;
    public static bool hasBindlessMDI = false;
    public static bool isNVCard = false;
    public static bool hasShadingLanguageInclude = false;

    public static NVShaderBufferLoad sbl;
    public static NVVertexBufferUnifiedMemory vbum;
    public static ExtBindableUniform extbu;
    public static NVCommandList cmdl;
    public static NVBindlessMultiDrawIndirect bmdi;
    public static ArbShadingLanguageInclude arbInclude;

    // MSAA resolve framebuffer (for MSAA -> regular texture)
    private uint resolveFbo;
    private uint resolveTex;

    // FXAA shader uniforms
    private int g_fxaa_texelStepLocation;
    private int g_fxaa_showEdgesLocation;
    private int g_fxaa_lumaThresholdLocation;
    private int g_fxaa_mulReduceLocation;
    private int g_fxaa_minReduceLocation;
    private int g_fxaa_maxSpanLocation;

    // SSAA shader uniforms
    private int g_ssaa_texelStepLocation;
    private int g_ssaa_factorLocation;
    private int g_ssaa_modeLocation;
    
    // CRT shader uniforms
    private int g_crt_maskTypeLocation;
    private int g_crt_curveLocation;
    private int g_crt_sharpnessLocation;
    private int g_crt_colorOffsetLocation;
    private int g_crt_brightnessLocation;
    private int g_crt_aspectLocation;
    private int g_crt_minScanlineThicknessLocation;
    private int g_crt_wobbleStrengthLocation;
    private int g_crt_timeLocation;
    private int g_crt_scanlineResLocation;

    private const float g_lumaThreshold = 0.5f;
    private const float g_mulReduceReciprocal = 8.0f;
    private const float g_minReduceReciprocal = 128.0f;
    private const float g_maxSpan = 8.0f;

    public static void initDedicatedGraphics() {
        // fuck integrated GPUs, we want the dedicated card
        try {
            if (Environment.Is64BitProcess) {
                NativeLibrary.Load("nvapi64.dll");
                NV2.NvAPI_Initialize();
            }
            else {
                NativeLibrary.Load("nvapi.dll");
                NV1.NvAPI_Initialize();
            }
        }
        catch (Exception e) {
            // nothing!
            Console.WriteLine("Well, apparently there is no nVidia");
        }
    }

    public void updateFramebuffers() {
        if (Settings.instance.framebufferEffects) {
            genFramebuffer();
        }
        else {
            deleteFramebuffer();
        }
    }

    private unsafe void genFramebuffer() {
        GL.DeleteFramebuffer(fbo);
        GL.DeleteFramebuffer(resolveFbo);

        // WIDTH CHECK
        if (width < 24) {
            width = 24;
        }

        if (height < 12) {
            height = 12;
        }

        var ssaaWidth = width * Settings.instance.effectiveScale;
        var ssaaHeight = height * Settings.instance.effectiveScale;
        var samples = Settings.instance.msaa;

        GL.Viewport(0, 0, (uint)ssaaWidth, (uint)ssaaHeight);

        if (samples > 1) {
            // Create MSAA framebuffer
            fbo = GL.CreateFramebuffer();

            // Create multisampled color texture
            GL.DeleteTexture(FBOtex);
            FBOtex = GL.CreateTexture(TextureTarget.Texture2DMultisample);
            GL.TextureStorage2DMultisample(FBOtex, (uint)samples, SizedInternalFormat.Rgba8,
                (uint)ssaaWidth, (uint)ssaaHeight, true);

            // Create multisampled depth buffer
            GL.DeleteRenderbuffer(depthBuffer);
            depthBuffer = GL.CreateRenderbuffer();
            GL.NamedRenderbufferStorageMultisample(depthBuffer, (uint)samples,
                InternalFormat.DepthComponent, (uint)ssaaWidth, (uint)ssaaHeight);

            // Attach to MSAA framebuffer
            GL.NamedFramebufferTexture(fbo, FramebufferAttachment.ColorAttachment0, FBOtex, 0);
            GL.NamedFramebufferRenderbuffer(fbo, FramebufferAttachment.DepthAttachment,
                RenderbufferTarget.Renderbuffer, depthBuffer);

            if (GL.CheckNamedFramebufferStatus(fbo, FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete) {
                throw new SkillIssueException("MSAA Framebuffer is not complete");
            }

            // Create resolve framebuffer for post-processing
            resolveFbo = GL.CreateFramebuffer();

            GL.DeleteTexture(resolveTex);
            resolveTex = GL.CreateTexture(TextureTarget.Texture2D);
            GL.TextureStorage2D(resolveTex, 1, SizedInternalFormat.Rgba8, (uint)ssaaWidth, (uint)ssaaHeight);
            GL.TextureParameter(resolveTex, TextureParameterName.TextureBaseLevel, 0);
            GL.TextureParameter(resolveTex, TextureParameterName.TextureMaxLevel, 0);
            GL.TextureParameter(resolveTex, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            GL.TextureParameter(resolveTex, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            GL.TextureParameter(resolveTex, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            GL.TextureParameter(resolveTex, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

            GL.NamedFramebufferTexture(resolveFbo, FramebufferAttachment.ColorAttachment0, resolveTex, 0);

            if (GL.CheckNamedFramebufferStatus(resolveFbo, FramebufferTarget.Framebuffer) !=
                GLEnum.FramebufferComplete) {
                throw new SkillIssueException("Resolve Framebuffer is not complete");
            }
        }
        else {
            // Regular framebuffer (no MSAA)
            fbo = GL.CreateFramebuffer();

            GL.DeleteTexture(FBOtex);
            FBOtex = GL.CreateTexture(TextureTarget.Texture2D);
            GL.TextureStorage2D(FBOtex, 1, SizedInternalFormat.Rgba8, (uint)ssaaWidth, (uint)ssaaHeight);
            GL.TextureParameter(FBOtex, TextureParameterName.TextureBaseLevel, 0);
            GL.TextureParameter(FBOtex, TextureParameterName.TextureMaxLevel, 0);
            GL.TextureParameter(FBOtex, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            GL.TextureParameter(FBOtex, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            GL.TextureParameter(FBOtex, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            GL.TextureParameter(FBOtex, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

            GL.DeleteRenderbuffer(depthBuffer);
            depthBuffer = GL.CreateRenderbuffer();
            GL.NamedRenderbufferStorage(depthBuffer, InternalFormat.DepthComponent, (uint)ssaaWidth,
                (uint)ssaaHeight);

            GL.NamedFramebufferTexture(fbo, FramebufferAttachment.ColorAttachment0, FBOtex, 0);
            GL.NamedFramebufferRenderbuffer(fbo, FramebufferAttachment.DepthAttachment,
                RenderbufferTarget.Renderbuffer, depthBuffer);

            if (GL.CheckNamedFramebufferStatus(fbo, FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete) {
                throw new SkillIssueException("Framebuffer is not complete");
            }

            resolveFbo = 0;
            resolveTex = 0;
        }

        graphics.fxaaShader.setUniform(g_fxaa_texelStepLocation, new Vector2(1.0f / ssaaWidth, 1.0f / ssaaHeight));

        graphics.ssaaShader.setUniform(g_ssaa_texelStepLocation, new Vector2(1.0f / ssaaWidth, 1.0f / ssaaHeight));
        graphics.ssaaShader.setUniform(g_ssaa_factorLocation, Settings.instance.ssaa);
        graphics.ssaaShader.setUniform(g_ssaa_modeLocation, Settings.instance.ssaaMode);

        // Set sample shading state based on settings
        if (Settings.instance.ssaaMode == 2 && Settings.instance.msaa > 1 && sampleShadingSupported) {
            GL.Enable(EnableCap.SampleShading);
            GL.MinSampleShading(1.0f); // force per-sample shading
            sampleShadingEnabled = true;
            // Enable A2C
            GL.Enable(EnableCap.SampleAlphaToCoverage);
        }
        else if (sampleShadingSupported) {
            GL.Disable(EnableCap.SampleShading);
            sampleShadingEnabled = false;
            GL.Disable(EnableCap.SampleAlphaToCoverage);
        }

        throwawayVAO = GL.CreateVertexArray();
    }

    private void deleteFramebuffer() {
        if (sampleShadingSupported) {
            GL.Disable(EnableCap.SampleShading); // disable per-sample shading
            sampleShadingEnabled = false;
            GL.Disable(EnableCap.SampleAlphaToCoverage);
        }

        GL.DeleteFramebuffer(fbo);
        GL.DeleteTexture(FBOtex);
        GL.DeleteRenderbuffer(depthBuffer);
        GL.DeleteFramebuffer(resolveFbo);
        GL.DeleteTexture(resolveTex);
        GL.DeleteVertexArray(throwawayVAO);
    }

    public partial class NV1 {
        [LibraryImport("nvapi.dll", EntryPoint = "nvapi_QueryInterface")]
        internal static partial int NvAPI_Initialize();
    }

    public static partial class NV2 {
        [LibraryImport("nvapi64.dll", EntryPoint = "nvapi_QueryInterface")]
        internal static partial int NvAPI_Initialize();
    }

    #if LAPTOP_SUPPORT
    public static void initDirectX() {
        unsafe {
            try {
                const bool forceDxvk = false;

                DXGI dxgi = null!;
                D3D11 d3d11 = null!;

                ComPtr<ID3D11Device> device = default;
                ComPtr<ID3D11DeviceContext> deviceContext = default;

                dxgi = DXGI.GetApi(window, forceDxvk);
                d3d11 = D3D11.GetApi(window, forceDxvk);

                // Create our D3D11 logical device.
                SilkMarshal.ThrowHResult
                (
                    d3d11.CreateDevice
                    (
                        default(ComPtr<IDXGIAdapter>),
                        D3DDriverType.Hardware,
                        Software: default,
                        (uint)CreateDeviceFlag.None,
                        null,
                        0,
                        D3D11.SdkVersion,
                        ref device,
                        null,
                        ref deviceContext
                    )
                );
                Console.Out.WriteLine("Successfully setup DirectX!");
            }
            catch (Exception e) {
                Console.Out.WriteLine("Couldn't setup DirectX!");
            }
        }
    }
    #endif
    
    public void setFullscreen(bool fullscreen) {
        if (fullscreen == (window.WindowState == WindowState.Fullscreen)) {
            return;
        }

        var windowMonitor = window.Monitor;
        if (windowMonitor == null) {
            return;
        }

        // temporarily remove resize handler to prevent issues during switch
        window.FramebufferResize -= resize;

        if (fullscreen) {
            var screenSize = windowMonitor.VideoMode.Resolution ?? windowMonitor.Bounds.Size;
            preFullscreenSize = window.Size;
            preFullscreenPosition = window.Position;
            preFullscreenState = window.WindowState;

            // Force Normal state first, then Fullscreen
            if (window.WindowState != WindowState.Normal) {
                window.WindowState = WindowState.Normal;
            }

            window.WindowState = WindowState.Fullscreen;
            window.Size = screenSize;
        }
        else {
            if (preFullscreenSize.X < 10 || preFullscreenSize.Y < 10 || preFullscreenState == WindowState.Fullscreen) {
                preFullscreenSize = windowMonitor.Bounds.Size * 2 / 3;
                preFullscreenPosition = windowMonitor.Bounds.Origin + new Vector2D<int>(50);
                preFullscreenState = WindowState.Normal;
            }

            // Always go to Normal first, then to the desired state
            window.WindowState = WindowState.Normal;
            window.Size = preFullscreenSize;
            window.Position = preFullscreenPosition;
            if (preFullscreenState != WindowState.Normal) {
                window.WindowState = preFullscreenState;
            }
        }

        // restore resize handler and trigger resize
        window.FramebufferResize += resize;
        resize(window.FramebufferSize);
    }
}