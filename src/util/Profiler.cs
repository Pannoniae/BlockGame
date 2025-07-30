using System.Diagnostics;
using BlockGame.GL.vertexformats;

namespace BlockGame.util;

public enum ProfileSectionName {
    Events, // Input events, window events
    Logic, // Update logic, world simulation
    World3D, // 3D world rendering  
    PostFX, // Post-processing, FXAA
    GUI, // GUI rendering
    Swap, // Buffer swap and VSYNC wait
    Other // Everything else
}

public record struct ProfileSection(ProfileSectionName section, float time) {
    public const int SECTION_COUNT = 7;
    
    public readonly ProfileSectionName section = section;
    public float time = time;
}

public readonly record struct ProfileData {
    public readonly ProfileSection[] sections = new ProfileSection[ProfileSection.SECTION_COUNT];

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
        sections[(int)section].time = time;
    }

    public static Color4b getColour(ProfileSectionName section) => section switch {
        ProfileSectionName.Events => new Color4b(150, 100, 255), // Purple
        ProfileSectionName.Logic => new Color4b(100, 150, 255), // Light blue
        ProfileSectionName.World3D => new Color4b(255, 100, 100), // Red
        ProfileSectionName.PostFX => new Color4b(255, 150, 0), // Orange
        ProfileSectionName.GUI => new Color4b(100, 255, 100), // Green
        ProfileSectionName.Swap => new Color4b(255, 100, 255), // Magenta
        ProfileSectionName.Other => new Color4b(200, 200, 200), // Gray
        _ => Color4b.White
    };
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
    
    public static string getSectionName(ProfileSectionName section) => section switch {
        ProfileSectionName.Events => "Events",
        ProfileSectionName.Logic => "Logic",
        ProfileSectionName.World3D => "World3D", 
        ProfileSectionName.PostFX => "PostFX",
        ProfileSectionName.GUI => "GUI",
        ProfileSectionName.Swap => "Swap",
        ProfileSectionName.Other => "Other",
        _ => "Unknown"
    };
}