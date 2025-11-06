using BlockGame.main;
using BlockGame.ui;
using BlockGame.util;
using BlockGame.util.log;
using BlockGame.world.block;
using MiniAudioEx;
using MiniAudioEx.Core.StandardAPI;
using MiniAudioEx.Native;
using Molten.DoublePrecision;

namespace BlockGame.snd;

public class SoundEngine : IDisposable {
    
    private readonly XMap<string, AudioClip> loadedSamples = [];
    private readonly XMap<string, List<AudioClip>> soundCategories = [];
    private readonly XRandom random = new();

    // SFX channels (fire-and-forget)
    private readonly SfxChannel[] sfxChannels = new SfxChannel[16];
    private int nextChannelIndex = 0;

    // Music sources (long-running, controllable)
    private readonly List<MusicSource> musicSources = [];
    private long lastKnock;

    // Spatial audio listener
    private readonly AudioListener listener;

    // The range for pitch variation (0.85 to 1.15 means 15% lower or higher pitch)
    private const float MIN_PITCH = 0.95f;
    private const float MAX_PITCH = 1.05f;

    public SoundEngine() {
        const uint SAMPLE_RATE = 44100;
        const uint CHANNELS = 2;

        // IF YOU CHANGE THIS stuff will jitter
        // so don't increase it too much, 2048 is crazy (like 46ms latency??)
        AudioContext.Initialize(SAMPLE_RATE, CHANNELS, 256);

        // initialize spatial audio listener
        listener = new AudioListener {
            Enabled = true,
            WorldUp = new Vector3f(0, 1, 0)
        };

        // initialize SFX channels
        for (int i = 0; i < sfxChannels.Length; i++) {
            sfxChannels[i] = new SfxChannel();
        }

        loadSounds();
    }

    private void loadSounds() {
        var snd = Game.assets.getPath("snd");
        if (!Directory.Exists(snd)) {
            throw new SkillIssueException("where the fuck is the sound dir?");
        }

        // recursively scan all subdirectories in snd/
        loadSoundsRecursive(snd, "");
    }

    private void loadSoundsRecursive(string dir, string prefix) {
        // load sounds from current directory
        var files = Directory.EnumerateFiles(dir).ToArray();
        if (files.Length > 0) {
            var categoryName = string.IsNullOrEmpty(prefix) ? Path.GetFileName(dir) : prefix;
            var clips = new List<AudioClip>();

            foreach (var file in files) {
                var clip = doLoad(file);
                if (clip != null) {
                    clips.Add(clip);
                }
            }

            if (clips.Count > 0) {
                soundCategories.Set(categoryName, clips);
            }
        }

        // recurse into subdirectories
        foreach (var subdir in Directory.EnumerateDirectories(dir)) {
            var subdirName = Path.GetFileName(subdir);
            var newPrefix = string.IsNullOrEmpty(prefix) ? subdirName : $"{prefix}/{subdirName}";
            loadSoundsRecursive(subdir, newPrefix);
        }
    }

    public AudioClip? load(string filepath) {
        var f = Game.assets.getPath(filepath);
        return doLoad(f);
    }

    private AudioClip? doLoad(string filepath) {
        if (!File.Exists(filepath)) {
            Log.warn($"SFX {filepath} does not exist!");
            throw new SoundException($"SFX {filepath} does not exist!");
        }

        try {
            var clip = new AudioClip(filepath);
            // strip ext
            var s = Path.ChangeExtension(Path.GetFileName(filepath), null);
            loadedSamples.Set(s, clip);
            return clip;
        }
        catch (Exception ex) {
            throw new SoundException($"Failed to load {filepath}: {ex}");
        }
    }

    /// <summary>
    /// Play a sound from the specified category using channel system
    /// </summary>
    public void play(string category, float pitch = 1.0f, float volume = 1.0f) {
        if (!soundCategories.TryGetValue(category, out var clips) || clips.Count == 0) {
            throw new SoundException($"No sounds loaded for category: {category}");
        }
        
        // pick random clip from category
        var clip = clips[random.Next(clips.Count)];
        
        // find free channel or use round-robin
        var channel = getFreeChannel();
        channel.play(clip, pitch, volume);
    }

    public void plays(string sound, float pitch = 1.0f, float volume = 1.0f) {
        if (!loadedSamples.TryGetValue(sound, out var clip)) {
            clip = doLoad(sound);
            loadedSamples.Set(sound, clip ?? throw new SoundException($"Failed to load sound: {sound}"));
        }

        // find free channel or use round-robin
        var channel = getFreeChannel();
        channel.play(clip, pitch, volume);
    }

    /// <summary>
    /// Play a sound from the specified category at a 3D position
    /// </summary>
    public void play(string category, Vector3D position, float pitch = 1.0f, float volume = 1.0f) {
        if (!soundCategories.TryGetValue(category, out var clips) || clips.Count == 0) {
            throw new SoundException($"No sounds loaded for category: {category}");
        }

        // pick random clip from category
        var clip = clips[random.Next(clips.Count)];

        // find free channel or use round-robin
        var channel = getFreeChannel();
        channel.play(clip, position, pitch, volume);
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

    public int getUsedChannels() {
        int count = 0;
        foreach (var channel in sfxChannels) {
            if (!channel.isFree) {
                count++;
            }
        }
        return count;
    }

    public int getTotalChannels() {
        return sfxChannels.Length;
    }

    /// <summary>
    /// Play music file, returns controllable MusicSource
    /// </summary>
    public MusicSource playMusic(string filepath) {

        filepath = Game.assets.getPath(filepath);

        if (!File.Exists(filepath)) {
            throw new SoundException($"Music file does not exist: {filepath}");
        }

        try {
            var musicSource = new MusicSource(filepath);
            musicSources.Add(musicSource);
            return musicSource;
        }
        catch (Exception ex) {
            throw new SoundException($"Failed to play music {filepath}: {ex}");
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

    public void updateSfxVolumes() {
        foreach (var channel in sfxChannels) {
            channel.updateVolume();
        }
    }

    public void updateMusicVolumes() {
        foreach (var music in musicSources) {
            music.updateVolume();
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

        // update listener position/direction for spatial audio
        if (Game.player != null! && Game.camera != null!) {
            var pos = Game.player.position;
            var forward = Game.camera.forward();

            listener.Position = new Vector3f((float)pos.X, (float)pos.Y, (float)pos.Z);
            listener.Direction = new Vector3f((float)forward.X, (float)forward.Y, (float)forward.Z);
            listener.Velocity = new Vector3f(
                (float)Game.player.velocity.X,
                (float)Game.player.velocity.Y,
                (float)Game.player.velocity.Z
            );
        }
    }

    public void playFootstep(SoundMaterial mat, float volume = 0.4f) {
        var cat = mat.stepCategory();
        play(cat, 1.0f, volume);
    }

    public void playFootstep(SoundMaterial mat, Vector3D position, float volume = 0.4f) {
        var cat = mat.stepCategory();
        play(cat, position, 1.0f, volume);
    }

    public void playBlockKnock(SoundMaterial mat, float volume = 0.3f) {
        var cat = mat.knockCategory();
        if (cat == mat.breakCategory()) {
            play(cat, 0.5f, volume);
        }
        else {
            play(cat, 1f, volume);
        }
    }

    public void playBlockKnock(SoundMaterial mat, Vector3D position, float volume = 0.3f) {
        var cat = mat.knockCategory();
        if (cat == mat.breakCategory()) {
            play(cat, position, 0.5f, volume);
        }
        else {
            play(cat, position, 1f, volume);
        }
    }

    public void playBlockBreak(SoundMaterial mat, float volume = 0.5f) {
        var cat = mat.breakCategory();
        if (cat == mat.stepCategory()) {
            play(cat, (getRandomPitch() * 0.1f) + 0.8f, volume);
        }
        else {
            play(cat, (getRandomPitch() * 0.1f) + 1f, volume);
        }
    }

    public void playBlockBreak(SoundMaterial mat, Vector3D position, float volume = 0.5f) {
        var cat = mat.breakCategory();
        if (cat == mat.stepCategory()) {
            play(cat, position, (getRandomPitch() * 0.1f) + 0.8f, volume);
        }
        else {
            play(cat, position, (getRandomPitch() * 0.1f) + 1f, volume);
        }
    }
    
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
    private float vol = 1.0f;

    public SfxChannel() {
        source = new AudioSource();
        source.End += () => { /* channel becomes free automatically via isFree */ };
    }

    public void play(AudioClip clip, float pitch = 1.0f, float volume = 1.0f) {
        source.Stop(); // interrupt if already playing
        source.Spatial = false; // disable spatial audio
        source.Pitch = pitch;
        vol = volume;
        source.Volume = volume * Settings.instance.sfxVolume;
        source.Play(clip);
    }

    public void play(AudioClip clip, Vector3D position, float pitch = 1.0f, float volume = 1.0f) {
        source.Stop(); // interrupt if already playing
        source.Spatial = true;
        source.Position = new Vector3f((float)position.X, (float)position.Y, (float)position.Z);
        source.MinDistance = 16.0f; // full volume within 16
        source.MaxDistance = 96.0f; // inaudible beyond 96 blocks
        source.AttenuationModel = AttenuationModel.Inverse;
        source.DopplerFactor = 0.0f;
        source.Pitch = pitch;
        vol = volume;
        source.Volume = volume * Settings.instance.sfxVolume;
        source.Play(clip);
    }

    public void updateVolume() {
        if (!isFree) {
            source.Volume = vol * Settings.instance.sfxVolume;
        }
    }

    public void dispose() {
        source?.Dispose();
    }
}

/// <summary>
/// Represents a controllable music source
/// </summary>
public class MusicSource : IDisposable {
    private readonly AudioSource source;
    private readonly AudioClip clip;
    private float vol = 1.0f;
    public bool isMusic => true;

    public MusicSource(string filepath) {
        clip = new AudioClip(filepath);
        source = new AudioSource();
        source.Volume = vol * Settings.instance.musicVolume;
        source.Play(clip);
        source.End += () => {
        };
    }

    public float volume {
        get => vol;
        set {
            vol = value;
            source.Volume = value * Settings.instance.musicVolume;
        }
    }

    public void updateVolume() {
        source.Volume = vol * Settings.instance.musicVolume;
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
        clip?.Dispose();
    }

    private void ReleaseUnmanagedResources() {
        dispose();
    }

    public void Dispose() {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~MusicSource() {
        Dispose();
    }
}

public class SoundException(string message) : Exception(message);