using System.Numerics;
using System.Runtime.InteropServices;
using BlockGame.ui;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.NV;
using Silk.NET.OpenGL.Legacy.Extensions.EXT;

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
    
    public static NVShaderBufferLoad sbl;
    public static NVVertexBufferUnifiedMemory vbum;
    public static ExtBindableUniform extbu;
    public static NVCommandList cmdl;
    public static NVBindlessMultiDrawIndirect bmdi;
    
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
            Console.Out.WriteLine("Well, apparently there is no nVidia");
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

        var ssaaWidth = width * Settings.instance.effectiveScale;
        var ssaaHeight = height * Settings.instance.effectiveScale;
        var samples = Settings.instance.msaa;

        GL.Viewport(0, 0, (uint)ssaaWidth, (uint)ssaaHeight);

        if (samples > 1) {
            // Create MSAA framebuffer
            fbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

            // Create multisampled color texture
            GL.DeleteTexture(FBOtex);
            FBOtex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DMultisample, FBOtex);
            GL.TexImage2DMultisample(TextureTarget.Texture2DMultisample, (uint)samples, InternalFormat.Rgba8,
                (uint)ssaaWidth, (uint)ssaaHeight, true);

            // Create multisampled depth buffer
            GL.DeleteRenderbuffer(depthBuffer);
            depthBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer);
            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, (uint)samples,
                InternalFormat.DepthComponent, (uint)ssaaWidth, (uint)ssaaHeight);

            // Attach to MSAA framebuffer
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2DMultisample, FBOtex, 0);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                RenderbufferTarget.Renderbuffer, depthBuffer);

            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete) {
                throw new Exception("MSAA Framebuffer is not complete");
            }

            // Create resolve framebuffer for post-processing
            resolveFbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, resolveFbo);

            GL.DeleteTexture(resolveTex);
            resolveTex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, resolveTex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)ssaaWidth, (uint)ssaaHeight, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, null);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, resolveTex, 0);

            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete) {
                throw new Exception("Resolve Framebuffer is not complete");
            }
        }
        else {
            // Regular framebuffer (no MSAA)
            fbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

            GL.DeleteTexture(FBOtex);
            FBOtex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, FBOtex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)ssaaWidth, (uint)ssaaHeight, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, null);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

            GL.DeleteRenderbuffer(depthBuffer);
            depthBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent, (uint)ssaaWidth,
                (uint)ssaaHeight);

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, FBOtex, 0);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                RenderbufferTarget.Renderbuffer, depthBuffer);

            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete) {
                throw new Exception("Framebuffer is not complete");
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
        }
        else if (sampleShadingSupported) {
            GL.Disable(EnableCap.SampleShading);
            sampleShadingEnabled = false;
        }

        throwawayVAO = GL.CreateVertexArray();
    }

    private void deleteFramebuffer() {
        if (sampleShadingSupported) {
            GL.Disable(EnableCap.SampleShading); // disable per-sample shading
            sampleShadingEnabled = false;
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
}