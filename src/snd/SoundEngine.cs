using BlockGame.util;
using MiniAudioEx;

namespace BlockGame;

public class SoundEngine : IDisposable {
    
    private Dictionary<string, AudioClip> loadedSamples = new();
    private Dictionary<string, List<AudioClip>> soundCategories = new();
    private XRandom random = new();
    
    // SFX channels (fire-and-forget)
    private SfxChannel[] sfxChannels = new SfxChannel[8];
    private int nextChannelIndex = 0;
    
    // Music sources (long-running, controllable)
    private List<MusicSource> musicSources = [];
    
    // The range for pitch variation (0.85 to 1.15 means 15% lower or higher pitch)
    private const float MIN_PITCH = 0.85f;
    private const float MAX_PITCH = 1.15f;

    public SoundEngine() {
        const uint SAMPLE_RATE = 44100;
        const uint CHANNELS = 2;
        
        AudioContext.Initialize(SAMPLE_RATE, CHANNELS);
        
        // initialize SFX channels
        for (int i = 0; i < sfxChannels.Length; i++) {
            sfxChannels[i] = new SfxChannel();
        }
        
        loadSounds();
    }

    private void loadSounds() {
        if (!Directory.Exists("snd")) {
            throw new SkillIssueException("where the fuck is the sound dir?");
        }
        
        // scan all subdirectories in snd/
        foreach (var categoryDir in Directory.EnumerateDirectories("snd")) {
            var categoryName = Path.GetFileName(categoryDir);
            var clips = new List<AudioClip>();
            
            foreach (var file in Directory.EnumerateFiles(categoryDir)) {
                var clip = load(file);
                if (clip != null) {
                    clips.Add(clip);
                }
            }
            
            if (clips.Count > 0) {
                soundCategories[categoryName] = clips;
            }
        }
    }

    public AudioClip? load(string filepath) {
        if (!File.Exists(filepath)) {
            Console.WriteLine($"SFX {filepath} does not exist!");
            throw new SoundException($"SFX {filepath} does not exist!");
        }

        try {
            var clip = new AudioClip(filepath);
            loadedSamples[filepath] = clip;
            return clip;
        }
        catch (Exception ex) {
            throw new SoundException($"Failed to load {filepath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Play a sound from the specified category using channel system
    /// </summary>
    public void playSfx(string category, float pitch = 1.0f, float volume = 1.0f) {
        if (!soundCategories.TryGetValue(category, out var clips) || clips.Count == 0) {
            throw new SoundException($"No sounds loaded for category: {category}");
        }
        
        // pick random clip from category
        var clip = clips[random.Next(clips.Count)];
        
        // find free channel or use round-robin
        var channel = getFreeChannel();
        channel.play(clip, pitch, volume);
    }
    
    private SfxChannel getFreeChannel() {
        // try to find free channel first
        foreach (var channel in sfxChannels) {
            if (channel.isFree) {
                return channel;
            }
        }
        
        // all busy, use round-robin
        var selectedChannel = sfxChannels[nextChannelIndex];
        nextChannelIndex = (nextChannelIndex + 1) % sfxChannels.Length;
        return selectedChannel;
    }

    /// <summary>
    /// Play music file, returns controllable MusicSource
    /// </summary>
    public MusicSource playMusic(string filepath) {
        if (!File.Exists(filepath)) {
            throw new SoundException($"Music file does not exist: {filepath}");
        }

        try {
            var musicSource = new MusicSource(filepath);
            musicSources.Add(musicSource);
            return musicSource;
        }
        catch (Exception ex) {
            throw new SoundException($"Failed to play music {filepath}: {ex.Message}");
        }
    }

    public void muteMusic() {
        foreach (var music in musicSources) {
            music.volume = 0.0f;
        }
    }
    
    public void unmuteMusic() {
        foreach (var music in musicSources) {
            music.volume = 1.0f;
        }
    }

    public float getRandomPitch() {
        return MIN_PITCH + (float)random.NextDouble() * (MAX_PITCH - MIN_PITCH);
    }

    /// <summary>
    /// Must be called regularly from the main thread for audio callbacks to work
    /// </summary>
    public void update() {
        AudioContext.Update();
    }

    // convenience methods using the new generic system
    public void playFootstep() => playSfx("steps", getRandomPitch());
    public void playBlockHit() => playSfx("hit", getRandomPitch());
    
    public long getMemoryUsage() {
        // MiniAudioEx doesn't provide direct memory usage stats lmfao, todo
        return -1;
    }

    private void ReleaseUnmanagedResources() {
        // dispose all SFX channels
        foreach (var channel in sfxChannels) {
            channel.dispose();
        }
        
        // dispose all music sources
        foreach (var music in musicSources) {
            music.dispose();
        }
        musicSources.Clear();

        // dispose all loaded clips
        foreach (var clip in loadedSamples.Values) {
            clip?.Dispose();
        }
        loadedSamples.Clear();
        soundCategories.Clear();

        // shutdown audio context
        AudioContext.Deinitialize();
    }

    public void Dispose() {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~SoundEngine() {
        ReleaseUnmanagedResources();
    }
}

/// <summary>
/// Represents a channel for fire-and-forget SFX playback
/// </summary>
public class SfxChannel {
    public AudioSource source;
    public bool isFree => !source.IsPlaying;
    
    public SfxChannel() {
        source = new AudioSource();
        source.End += () => { /* channel becomes free automatically via isFree */ };
    }
    
    public void play(AudioClip clip, float pitch = 1.0f, float volume = 1.0f) {
        source.Stop(); // interrupt if already playing
        source.Pitch = pitch;
        source.Volume = volume;
        source.Play(clip);
    }
    
    public void dispose() {
        source?.Dispose();
    }
}

/// <summary>
/// Represents a controllable music source
/// </summary>
public class MusicSource {
    public AudioSource source;
    public bool isMusic => true;
    
    public MusicSource(string filepath) {
        var clip = new AudioClip(filepath);
        source = new AudioSource();
        source.Play(clip);
    }
    
    public float volume {
        get => source.Volume;
        set => source.Volume = value;
    }
    
    public float pitch {
        get => source.Pitch;
        set => source.Pitch = value;
    }
    
    public bool loop {
        get => source.Loop;
        set => source.Loop = value;
    }
    
    public void dispose() {
        source?.Dispose();
    }
}

public class SoundException(string message) : Exception(message);