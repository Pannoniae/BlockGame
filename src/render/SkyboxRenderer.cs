using System.Numerics;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using Silk.NET.OpenGL;
using PrimitiveType = Silk.NET.OpenGL.PrimitiveType;

namespace BlockGame.render;

public class SkyboxRenderer : InstantDraw<VertexTinted> {
    
    public int uInverseView;
    public int uInverseProjection;


    public int usunPosition;//:Vector3 = new Vector3();
    public int ucameraPos;//:Vector3 = new Vector3();
    public int uturbidity;//:Float;
    public int urayleigh;//:Float;
    public int umieCoefficient;//:Float;
    public int umieDirectionalG;//:Float;
    public int uluminance;//:Float;
    public int urefractiveIndex;//:Float;
    public int unumMolecules;//:Float;
    public int udepolarizationFactor;//:Float;
    public int uprimaries;//:Vector3 = new Vector3();
    public int umieKCoefficient;//:Vector3 = new Vector3();
    public int urayleighZenithLength;//:Float;
    public int umieV;//:Float;
    public int umieZenithLength;//:Float;
    public int usunIntensityFactor;//:Float;
    public int usunIntensityFalloffSteepness;//:Float;
    public int usunAngularDiameterDegrees;//:Float;
    public int utonemapWeighting;//:Float;
    
    
    
    // Skybox parameters

    public Vector3 sunPosition;
    public Vector3 cameraPos;
    public float turbidity;
    public float rayleigh;
    public float mieCoefficient;
    public float mieDirectionalG;
    public float luminance;
    public float azimuth;
    public float refractiveIndex;
    public float numMolecules;
    public float depolarizationFactor;
    public Vector3 primaries;
    public Vector3 mieKCoefficient;
    public float rayleighZenithLength;
    public float mieV;
    public float mieZenithLength;
    public float sunIntensityFactor;
    public float sunIntensityFalloffSteepness;
    public float sunAngularDiameterDegrees;
    public float tonemapWeighting;
    
    public float inclination;
    
    public override bool hasFog => false;

    public SkyboxRenderer() : base(6) {
        // only need 4 vertices for a fullscreen quad
        // which is 6 because we're using indices, oh well
    }

    public override void setup() {
        base.setup();
        
        
        instantShader = new GL.Shader(GL, nameof(instantShader), "shaders/sky/skybox.vert", "shaders/sky/skybox.frag");
        
        uInverseView = instantShader.getUniformLocation("uInverseView");
        uInverseProjection = instantShader.getUniformLocation("uInverseProjection");
        
        usunPosition = instantShader.getUniformLocation(nameof(sunPosition));
        ucameraPos = instantShader.getUniformLocation(nameof(cameraPos));
        uturbidity = instantShader.getUniformLocation(nameof(turbidity));
        urayleigh = instantShader.getUniformLocation(nameof(rayleigh));
        umieCoefficient = instantShader.getUniformLocation(nameof(mieCoefficient));
        umieDirectionalG = instantShader.getUniformLocation(nameof(mieDirectionalG));
        uluminance = instantShader.getUniformLocation(nameof(luminance));
        urefractiveIndex = instantShader.getUniformLocation(nameof(refractiveIndex));
        unumMolecules = instantShader.getUniformLocation(nameof(numMolecules));
        udepolarizationFactor = instantShader.getUniformLocation(nameof(depolarizationFactor));
        uprimaries = instantShader.getUniformLocation(nameof(primaries));
        umieKCoefficient = instantShader.getUniformLocation(nameof(mieKCoefficient));
        urayleighZenithLength = instantShader.getUniformLocation(nameof(rayleighZenithLength));
        umieV = instantShader.getUniformLocation(nameof(mieV));
        umieZenithLength = instantShader.getUniformLocation(nameof(mieZenithLength));
        usunIntensityFactor = instantShader.getUniformLocation(nameof(sunIntensityFactor));
        usunIntensityFalloffSteepness = instantShader.getUniformLocation(nameof(sunIntensityFalloffSteepness));
        usunAngularDiameterDegrees = instantShader.getUniformLocation(nameof(sunAngularDiameterDegrees));
        utonemapWeighting = instantShader.getUniformLocation(nameof(tonemapWeighting));
        
        
		//sunPosition = new(0, -700000, 0);
		sunPosition = new(0, 0, 0);
		//cameraPos = new(100000.0f, -40000.0f, 0.0f);
		cameraPos = new(1.0f, 1.0f, 1.0f);
		turbidity = 2.0f;
		rayleigh = 1.0f;
		mieCoefficient = 0.005f;
		mieDirectionalG = 0.8f;
        
        
		luminance = 1.0f;
		inclination = 0.49f;
		azimuth = 0.25f;

		// Refractive index of air
		refractiveIndex = 1.0003f;
		
		// Number of molecules per unit volume for air at 288.15K and 1013mb (sea level -45 celsius)
		numMolecules = 2.542e25f;
		
		// Depolarization factor for air wavelength of primaries
		depolarizationFactor = 0.035f;
		primaries= new(6.8e-7f, 5.5e-7f, 4.5e-7f);
		
		// Mie, K coefficient for the primaries
		mieKCoefficient = new(0.686f, 0.678f, 0.666f);
		mieV = 4.0f;
		
		// Optical length at zenith for molecules
		rayleighZenithLength = 8.4e3f;
		mieZenithLength = 1.25e3f;
		
		// Sun intensity factors
		sunIntensityFactor = 1000.0f;
		sunIntensityFalloffSteepness = 1.5f;
		
		// Visual size of sun
		//sunAngularDiameterDegrees = 0.0093333f;
        
        // SLIGHTLY smaller than the real sun
		sunAngularDiameterDegrees = 0.0093333f * 8;
		
		// W factor in tonemap calculation
		tonemapWeighting = 9.50f;
	}

    public override void format() {
        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);

        GL.VertexAttribFormat(0, 3, VertexAttribType.Float, false, 0);
        GL.VertexAttribFormat(1, 4, VertexAttribType.UnsignedByte, true, 0 + 6 * sizeof(ushort));

        GL.VertexAttribBinding(0, 0);
        GL.VertexAttribBinding(1, 0);

        GL.BindVertexBuffer(0, VBO, 0, 8 * sizeof(ushort));
    }

    public void renderSkybox(Matrix4x4 viewMatrix, Matrix4x4 projMatrix, float dayPercent, Vector3 cameraPosition) {
        instantShader.use();
        
        // Calculate sun position to exactly match WorldRenderer.renderSky
        // dayPercent = 0 is sunrise (sun at horizon), so phase shift by -90°
        float sunAngle = dayPercent * MathF.PI * 2 - MathF.PI / 2; // -90° phase shift
        
        // Replicate the exact rotation: start with sun at (1, 0, 0) for sunrise and rotate around Z
        sunPosition = new Vector3(
            -MathF.Sin(sunAngle) * 400000.0f,  // X component after Z rotation
            MathF.Cos(sunAngle) * 400000.0f,   // Y component after Z rotation  
            0                                  // Z stays 0 (no forward/back movement)
        );
        
        // Update camera position
        cameraPos = cameraPosition;
        
        // Calculate inverse matrices for world space reconstruction
        Matrix4x4.Invert(viewMatrix, out Matrix4x4 inverseView);
        Matrix4x4.Invert(projMatrix, out Matrix4x4 inverseProjection);
        
        // Set matrix uniforms
        instantShader.setUniform(uInverseView, inverseView);
        instantShader.setUniform(uInverseProjection, inverseProjection);
        instantShader.setUniform(usunPosition, sunPosition);
        instantShader.setUniform(ucameraPos, cameraPos);
        
        // Set all atmospheric scattering parameters
        instantShader.setUniform(uturbidity, turbidity);
        instantShader.setUniform(urayleigh, rayleigh);
        instantShader.setUniform(umieCoefficient, mieCoefficient);
        instantShader.setUniform(umieDirectionalG, mieDirectionalG);
        instantShader.setUniform(uluminance, luminance);
        instantShader.setUniform(urefractiveIndex, refractiveIndex);
        instantShader.setUniform(unumMolecules, numMolecules);
        instantShader.setUniform(udepolarizationFactor, depolarizationFactor);
        instantShader.setUniform(uprimaries, primaries);
        instantShader.setUniform(umieKCoefficient, mieKCoefficient);
        instantShader.setUniform(urayleighZenithLength, rayleighZenithLength);
        instantShader.setUniform(umieV, mieV);
        instantShader.setUniform(umieZenithLength, mieZenithLength);
        instantShader.setUniform(usunIntensityFactor, sunIntensityFactor);
        instantShader.setUniform(usunIntensityFalloffSteepness, sunIntensityFalloffSteepness);
        instantShader.setUniform(usunAngularDiameterDegrees, sunAngularDiameterDegrees);
        instantShader.setUniform(utonemapWeighting, tonemapWeighting);
        
        // disable depth writes for skybox
        GL.DepthMask(false);
        GL.Disable(EnableCap.CullFace);
        
        begin(PrimitiveType.Quads);
        
        // fullscreen quad covering the far clip plane
        const float z = 0.8f; // just before far clip
        addVertex(new VertexTinted(-1, -1, z, Molten.Color.White));
        addVertex(new VertexTinted(1, -1, z, Molten.Color.White));
        addVertex(new VertexTinted(1, 1, z, Molten.Color.White));
        addVertex(new VertexTinted(-1, 1, z, Molten.Color.White));
        
        end();
        
        GL.DepthMask(true);
        GL.Enable(EnableCap.CullFace);
    }
}