using BlockGame.util;
using MiniAudioEx;

namespace BlockGame;

public class SoundEngine : IDisposable {

    private List<Sound> sounds = [];
    private Dictionary<string, AudioClip> loadedSamples = new();
    private XRandom random = new();
    
    private List<AudioClip> footstepSounds = [];
    private List<AudioClip> blockHitSounds = [];
    //private FMGenerator sine;
    //private AudioSource sinesrc;

    // The range for pitch variation (0.85 to 1.15 means 15% lower or higher pitch)
    private const float MIN_PITCH = 0.85f;
    private const float MAX_PITCH = 1.15f;

    public SoundEngine() {
        const uint SAMPLE_RATE = 44100;
        const uint CHANNELS = 2;
        
        // use default device
        //var devices = AudioContext.GetDevices();
        //foreach (var device in devices) {
        //    Console.Out.WriteLine(device.Name);
        //}
        AudioContext.Initialize(SAMPLE_RATE, CHANNELS);
        loadSounds();
    }

    private void loadSounds() {
        // load all steps in the snd/steps directory
        if (Directory.Exists("snd/steps")) {
            foreach (var file in Directory.EnumerateFiles("snd/steps")) {
                var clip = load(file);
                if (clip != null) {
                    footstepSounds.Add(clip);
                }
            }
        }
        
        // load all block hits in the snd/hit directory
        if (Directory.Exists("snd/hit")) {
            foreach (var file in Directory.EnumerateFiles("snd/hit")) {
                var clip = load(file);
                if (clip != null) {
                    blockHitSounds.Add(clip);
                }
            }
        }
    }

    private AudioClip? load(string filepath) {
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

    private Sound play(string filepath, float pitch = 1.0f) {
        if (!loadedSamples.TryGetValue(filepath, out AudioClip? clip)) {
            clip = load(filepath);
            if (clip == null) {
                throw new SoundException($"Failed to load audio clip: {filepath}");
            }
        }

        var source = new AudioSource();
        source.Pitch = pitch;
        source.Play(clip);

        var sound = new Sound(source);
        sounds.Add(sound);
        return sound;
    }

    private void play(AudioClip clip, float pitch = 1.0f) {
        if (clip == null) {
            throw new SoundException("Audio clip is null");
        }
        Console.Out.WriteLine($"Loaded {footstepSounds.Count} footstep sounds");
        
        var source = new AudioSource();
        source.Pitch = pitch;
        source.Play(clip);

        sounds.Add(new Sound(source));
    }

    public Sound playMusic(string name) {
        if (!File.Exists(name)) {
            throw new SoundException($"Music file does not exist: {name}");
        }

        try {
            var clip = new AudioClip(name);
            var source = new AudioSource();
            source.Play(clip);
            
            var sound = new Sound(source, isMusic: true);
            sounds.Add(sound);
            
            
            // play sine wave
            //sinesrc = new AudioSource();
            //sine = new FMGenerator(WaveType.Sine, 440.0f, 0.5f);
            //sinesrc.AddGenerator(sine);
            //sinesrc.Play();
            
            return sound;
        }
        catch (Exception ex) {
            throw new SoundException($"Failed to play music {name}: {ex.Message}");
        }
    }

    public void muteMusic() {
        foreach (var sound in sounds) {
            if (sound.isMusic && sound.source != null) {
                sound.source.Volume = 0.0f;
            }
        }
        //sinesrc.Volume = 0.0f; // also mute the sine wave
    }
    
    public void unmuteMusic() {
        foreach (var sound in sounds) {
            if (sound.isMusic && sound.source != null) {
                sound.source.Volume = 1.0f;
            }
        }
        //sinesrc.Volume = 1.0f; // unmute the sine wave
    }

    public void setLoop(Sound sound, bool loop) {
        if (sound?.source != null) {
            sound.source.Loop = loop;
        }
    }

    public void setPitch(Sound sound, float pitch) {
        if (sound?.source != null) {
            sound.source.Pitch = pitch;
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

    public Sound playFootstep() {
        if (footstepSounds.Count == 0) {
            throw new SoundException("No footstep sounds loaded");
        }

        var clip = footstepSounds[random.Next(footstepSounds.Count)];
        var pitch = getRandomPitch();
        play(clip, pitch);

        // Return a dummy sound since we don't track individual SFX playbacks
        return new Sound(null);
    }

    public Sound playBlockHit() {
        if (blockHitSounds.Count == 0) {
            throw new SoundException("No block hit sounds loaded");
        }

        var clip = blockHitSounds[random.Next(blockHitSounds.Count)];
        var pitch = getRandomPitch();
        play(clip, pitch);

        // Return a dummy sound since we don't track individual SFX playbacks  
        return new Sound(null);
    }
    
    public long getMemoryUsage() {
        // MiniAudioEx doesn't provide direct memory usage stats
        return -1;
    }

    private void ReleaseUnmanagedResources() {
        // Dispose all sounds
        foreach (var sound in sounds) {
            sound.source?.Dispose();
        }
        sounds.Clear();

        // Dispose all loaded clips
        foreach (var clip in loadedSamples.Values) {
            clip?.Dispose();
        }
        loadedSamples.Clear();

        // Shutdown audio context
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

public class Sound {
    public AudioSource? source;
    public bool isMusic;

    public Sound(AudioSource? audioSource, bool isMusic = false) {
        source = audioSource;
        this.isMusic = isMusic;
    }
}

public class SoundException(string message) : Exception(message);