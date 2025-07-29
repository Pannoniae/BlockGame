using System.Diagnostics;
using BlockGame.GL.vertexformats;

namespace BlockGame.util;

public enum ProfileSection {
    Events,     // Input events, window events
    Logic,      // Update logic, world simulation
    World3D,    // 3D world rendering  
    PostFX,     // Post-processing, FXAA
    GUI,        // GUI rendering
    Swap,       // Buffer swap and VSYNC wait
    Other       // Everything else
}

public struct ProfileData {
    public float eventsTime;
    public float logicTime;
    public float world3DTime;
    public float postFXTime;
    public float guiTime;
    public float swapTime;
    public float otherTime;
    
    public float total => eventsTime + logicTime + world3DTime + postFXTime + guiTime + swapTime + otherTime;
    
    public float getTime(ProfileSection section) => section switch {
        ProfileSection.Events => eventsTime,
        ProfileSection.Logic => logicTime,
        ProfileSection.World3D => world3DTime,
        ProfileSection.PostFX => postFXTime,
        ProfileSection.GUI => guiTime,
        ProfileSection.Swap => swapTime,
        ProfileSection.Other => otherTime,
        _ => 0f
    };
    
    public void setTime(ProfileSection section, float time) {
        switch (section) {
            case ProfileSection.Events: eventsTime = time; break;
            case ProfileSection.Logic: logicTime = time; break;
            case ProfileSection.World3D: world3DTime = time; break;
            case ProfileSection.PostFX: postFXTime = time; break;
            case ProfileSection.GUI: guiTime = time; break;
            case ProfileSection.Swap: swapTime = time; break;
            case ProfileSection.Other: otherTime = time; break;
        }
    }
    
    public static Color4b getColour(ProfileSection section) => section switch {
        ProfileSection.Events => new Color4b(150, 100, 255),   // Purple
        ProfileSection.Logic => new Color4b(100, 150, 255),    // Light blue
        ProfileSection.World3D => new Color4b(255, 100, 100),  // Red
        ProfileSection.PostFX => new Color4b(255, 150, 0),     // Orange
        ProfileSection.GUI => new Color4b(100, 255, 100),      // Green
        ProfileSection.Swap => new Color4b(255, 100, 255),     // Magenta
        ProfileSection.Other => new Color4b(200, 200, 200),    // Gray
        _ => Color4b.White
    };
}

public class Profiler {
    private readonly Stopwatch stopwatch = new();
    private ProfileSection currentSection = ProfileSection.Other;
    private ProfileData currentFrame;
    private float sectionStartTime;
    
    public void startFrame() {
        currentFrame = new ProfileData();
        stopwatch.Restart();
        sectionStartTime = 0f;
        currentSection = ProfileSection.Other;
    }
    
    public void section(ProfileSection section) {
        var now = (float)stopwatch.Elapsed.TotalMilliseconds;
        
        // Add time spent in previous section
        if (currentSection != section) {
            var elapsed = now - sectionStartTime;
            currentFrame.setTime(currentSection, currentFrame.getTime(currentSection) + elapsed);
            sectionStartTime = now;
            currentSection = section;
        }
    }
    
    public ProfileData endFrame() {
        var now = (float)stopwatch.Elapsed.TotalMilliseconds;
        
        // Add remaining time to current section
        var elapsed = now - sectionStartTime;
        currentFrame.setTime(currentSection, currentFrame.getTime(currentSection) + elapsed);
        
        return currentFrame;
    }
    
    public static string getSectionName(ProfileSection section) => section switch {
        ProfileSection.Events => "Events",
        ProfileSection.Logic => "Logic",
        ProfileSection.World3D => "World3D", 
        ProfileSection.PostFX => "PostFX",
        ProfileSection.GUI => "GUI",
        ProfileSection.Swap => "Swap",
        ProfileSection.Other => "Other",
        _ => "Unknown"
    };
}