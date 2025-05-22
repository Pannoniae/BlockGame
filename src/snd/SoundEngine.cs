using BlockGame.util;
using Un4seen.Bass;

namespace BlockGame.snd;

public class SoundEngine : IDisposable {

    private List<Sound> sounds = [];
    private List<int> channels = [];
    private Dictionary<string, int> loadedSamples = new();
    private XRandom random = new();
    
    private List<Sound> footstepSounds = [];
    private List<Sound> blockHitSounds = [];

    // Default playback frequency (44100Hz is standard)
    private const float DEFAULT_FREQUENCY = 44100f;
    // The range for pitch variation (0.85 to 1.15 means 15% lower or higher pitch)
    private const float MIN_PITCH = 0.85f;
    private const float MAX_PITCH = 1.15f;

    public SoundEngine() {
        // don't use local variables, they go out of scope so nothing plays..... hold them statically
        // init BASS using the default output device
        if (Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero)) {
            // create a stream channel from a file
            int res = Bass.BASS_PluginLoad("bassflac");
            if (res == 0) {
                // error loading the plugin
                Console.WriteLine("Plugin error: {0}", Bass.BASS_ErrorGetCode());
            }
        }
        else {
            throw new SoundException($"BASS_Init failed: {Bass.BASS_ErrorGetCode()}");
        }

        loadSounds();
    }
    private void loadSounds() {
        // load all steps in the snd/steps directory
        foreach (var file in Directory.EnumerateFiles("snd/steps")) {
            footstepSounds.Add(load(file)!);
        }
        // load all block hits in the snd/hit directory
        foreach (var file in Directory.EnumerateFiles("snd/hit")) {
            blockHitSounds.Add(load(file)!);
        }
    }

    private Sound? load(string filepath) {
        // if it doesn't exist, warn
        if (!File.Exists(filepath)) {
            Console.WriteLine($"SFX {filepath} does not exist!");
            throw new SoundException($"SFX {filepath} does not exist!");
        }

        int sample = Bass.BASS_SampleLoad(filepath, 0, 0, 3, BASSFlag.BASS_DEFAULT);
        if (sample == 0) {
            throw new SoundException($"BASS_SampleLoad failed for {filepath}: {Bass.BASS_ErrorGetCode()}");
        }
        // set the max number of channels to 32
        var flags = Bass.BASS_SampleGetInfo(sample);
        if (flags == null) {
            throw new SoundException($"BASS_SampleGetInfo failed for {filepath}: {Bass.BASS_ErrorGetCode()}");
        }
        flags.max = 8;
        Bass.BASS_SampleSetInfo(sample, flags);
        
        loadedSamples[filepath] = sample;
        return new Sound(sample);
    }

    private Sound play(string filepath, float pitch = 1.0f) {
        if (!loadedSamples.TryGetValue(filepath, out int sample)) {
            load(filepath);
            sample = loadedSamples[filepath];
        }

        int channel = Bass.BASS_SampleGetChannel(sample, BASSFlag.BASS_STREAM_AUTOFREE | BASSFlag.BASS_SAMCHAN_NEW);
        if (channel == 0) {
            throw new SoundException($"BASS_SampleGetChannel failed: {Bass.BASS_ErrorGetCode()}");
        }

        Bass.BASS_ChannelSetAttribute(channel, BASSAttribute.BASS_ATTRIB_FREQ, DEFAULT_FREQUENCY * pitch);

        if (!Bass.BASS_ChannelPlay(channel, true)) {
            throw new SoundException($"BASS_ChannelPlay failed: {Bass.BASS_ErrorGetCode()}");
        }

        channels.Add(channel);
        return new Sound(channel);
    }

    private void play(Sound sound, float pitch = 1.0f) {
        if (sound.stream == 0) {
            throw new SoundException($"BASS_SampleGetChannel failed: {Bass.BASS_ErrorGetCode()}");
        }
        
        int channel = Bass.BASS_SampleGetChannel(sound.stream, BASSFlag.BASS_STREAM_AUTOFREE | BASSFlag.BASS_SAMCHAN_NEW);
        if (channel == 0) {
            throw new SoundException($"BASS_SampleGetChannel failed: {Bass.BASS_ErrorGetCode()}");
        }

        Bass.BASS_ChannelSetAttribute(channel, BASSAttribute.BASS_ATTRIB_FREQ, DEFAULT_FREQUENCY * pitch);

        if (!Bass.BASS_ChannelPlay(channel, true)) {
            throw new SoundException($"BASS_ChannelPlay failed: {Bass.BASS_ErrorGetCode()}");
        }
    }

    public Sound playMusic(string name) {
        // create a stream channel from a file
        int stream = Bass.BASS_StreamCreateFile(name, 0, 0, BASSFlag.BASS_DEFAULT);
        if (stream == 0) {
            throw new SoundException($"BASS_StreamCreateFile failed: {Bass.BASS_ErrorGetCode()}");
        }
        // play the stream
        if (!Bass.BASS_ChannelPlay(stream, false)) {
            throw new SoundException($"BASS_ChannelPlay failed: {Bass.BASS_ErrorGetCode()}");
        }
        var sound = new Sound(stream);
        sounds.Add(sound);
        return sound;
    }

    public void setLoop(Sound sound, bool loop) {
        Bass.BASS_ChannelFlags(sound.stream, loop ? BASSFlag.BASS_SAMPLE_LOOP : 0, BASSFlag.BASS_SAMPLE_LOOP);
    }

    public void setPitch(Sound sound, float pitch) {
        var freq = 0.0f;
        Bass.BASS_ChannelGetAttribute(sound.stream, BASSAttribute.BASS_ATTRIB_FREQ, ref freq);

        // Apply the pitch multiplier to the frequency
        float newFreq = DEFAULT_FREQUENCY * pitch;

        // Set the new frequency
        Bass.BASS_ChannelSetAttribute(sound.stream, BASSAttribute.BASS_ATTRIB_FREQ, newFreq);
    }

    public float getRandomPitch() {
        return MIN_PITCH + (float)random.NextDouble() * (MAX_PITCH - MIN_PITCH);
    }

    public Sound playFootstep() {
        var sound = footstepSounds[random.Next(footstepSounds.Count)];
        play(sound);
        
        setPitch(sound, getRandomPitch());

        return sound;
    }

    public Sound playBlockHit() {
        var sound = blockHitSounds[random.Next(blockHitSounds.Count)];
        play(sound);
        
        setPitch(sound, getRandomPitch());

        return sound;
    }
    
    public long getMemoryUsage() {
        return -1;
    }

    private void ReleaseUnmanagedResources() {
        // free all streams
        foreach (var sound in sounds) {
            Bass.BASS_StreamFree(sound.stream);
        }
        Bass.BASS_Free();
    }
    public void Dispose() {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }
    ~SoundEngine() {
        ReleaseUnmanagedResources();
    }
}

public class Sound(int stream) {
    public int stream = stream;
}

public class SoundException(string message) : Exception(message);
