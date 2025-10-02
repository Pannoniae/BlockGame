using System.Diagnostics;
using System.Runtime.CompilerServices;
using BlockGame.main;

namespace BlockGame.util;

public enum ProfileSectionName {
    Events, // Input events, window events
    Clear, // Clear screen, depth buffer
    Logic, // Update logic, world simulation
    World3D, // 3D world rendering  
    PostFX, // Post-processing, FXAA
    GUI, // GUI rendering
    Swap, // Buffer swap and VSYNC wait
    Other // Everything else
}

public record struct ProfileSection(ProfileSectionName section, float time) {
    public const int SECTION_COUNT = 8;
    
    public readonly ProfileSectionName section = section;
    public float time = time;
}

public record struct ProfileData {
    
    public ProfileSections sections;

    public ProfileData() {
        
    }

    public float total {
        get {
            float totalTime = 0f;
            foreach (var section in sections) {
                totalTime += section.time;
            }
            return totalTime;
        }
    }

    public float getTime(ProfileSectionName section) {
        return sections[(int)section].time;
    }

    public void setTime(ProfileSectionName section, float time) {
        ref var profileSection = ref sections[(int)section];
        profileSection.time = time;
    }

    public static Color4b getColour(ProfileSectionName section) => section switch {
        ProfileSectionName.Events => new Color4b(150, 100, 255), // Purple
        ProfileSectionName.Clear => new Color4b(100, 255, 200), // Light green
        ProfileSectionName.Logic => new Color4b(100, 150, 255), // Light blue
        ProfileSectionName.World3D => new Color4b(255, 100, 100), // Red
        ProfileSectionName.PostFX => new Color4b(255, 150, 0), // Orange
        ProfileSectionName.GUI => new Color4b(100, 255, 100), // Green
        ProfileSectionName.Swap => new Color4b(255, 100, 255), // Magenta
        ProfileSectionName.Other => new Color4b(200, 200, 200), // Gray
        _ => Color4b.White
    };
}

[InlineArray(ProfileSection.SECTION_COUNT)]
public struct ProfileSections
{
    private ProfileSection section;
}

public class Profiler {
    private readonly Stopwatch stopwatch = new();
    private ProfileSectionName currentSection = ProfileSectionName.Other;
    private ProfileData currentFrame;
    private float sectionStartTime;

    public void startFrame() {
        currentFrame = new ProfileData();
        stopwatch.Restart();
        sectionStartTime = 0f;
        currentSection = ProfileSectionName.Other;
    }

    public void section(ProfileSectionName section) {
        var now = (float)stopwatch.Elapsed.TotalMilliseconds;
        
        // pop previous group
        Game.graphics.popGroup();

        // Add time spent in previous section
        if (currentSection != section) {
            var elapsed = now - sectionStartTime;
            currentFrame.setTime(currentSection, currentFrame.getTime(currentSection) + elapsed);
            sectionStartTime = now;
            currentSection = section;
        }
        
        Game.graphics.pushGroup(getSectionName(section), ProfileData.getColour(section));
        
        // add debug marker
        //Game.GL.DebugMessageInsert(DebugSource.DebugSourceApplication, DebugType.DebugTypeMarker, 0, DebugSeverity.DebugSeverityNotification, uint.MaxValue, $"Section: {getSectionName(section)}");
    }

    public ProfileData endFrame() {
        var now = (float)stopwatch.Elapsed.TotalMilliseconds;

        // Add remaining time to current section
        var elapsed = now - sectionStartTime;
        currentFrame.setTime(currentSection, currentFrame.getTime(currentSection) + elapsed);
        
        Game.graphics.popGroup();

        return currentFrame;
    }
    
    public static string getSectionName(ProfileSectionName section) => section switch {
        ProfileSectionName.Events => "Events",
        ProfileSectionName.Clear => "Clear",
        ProfileSectionName.Logic => "Logic",
        ProfileSectionName.World3D => "World3D", 
        ProfileSectionName.PostFX => "PostFX",
        ProfileSectionName.GUI => "GUI",
        ProfileSectionName.Swap => "Swap",
        ProfileSectionName.Other => "Other",
        _ => "Unknown"
    };
}