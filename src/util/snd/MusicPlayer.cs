using BlockGame.util.log;

namespace BlockGame.util.snd;

/**
 * Handles sparse random music playback.
 * Plays random tracks from assets/music/ with long delays between them.
 */
public class MusicPlayer {
    private readonly SoundEngine snd;
    private readonly XRandom random = new();
    private readonly List<string> musicFiles = [];

    private MusicSource? currentTrack;
    private double timeSinceLastTrack;
    private double nextTrackDelay;
    private bool trackPlaying;

    // delays: 3-8 minutes between tracks
    private const double MIN_DELAY_SECONDS = 180.0;
    private const double MAX_DELAY_SECONDS = 480.0;

    public static readonly Dictionary<string, float> volumes = new() {
        // per-track volume adjustments (relative to global music volume)
        ["OrganCrumhorns1"] = 0.5f,
        ["Clavicordium-Forest-Lilt"] = 0.5f,
        ["DesolateArp"] = 0.5f,
    };

    public MusicPlayer(SoundEngine snd) {
        this.snd = snd;
        loadMusicFiles();

        // play first track immediately
        //playRandomTrack();
    }

    private void loadMusicFiles() {
        var musicDir = Assets.getPath("music");
        if (!Directory.Exists(musicDir)) {
            Log.warn("Music directory doesn't exist, no music will play");
            return;
        }

        // load all supported audio files
        var supportedExtensions = new[] { "*.flac", "*.mp3", "*.ogg", "*.wav" };
        foreach (var ext in supportedExtensions) {
            var file = Directory.GetFiles(musicDir, ext, SearchOption.AllDirectories);
            // strip the asset path prefix (assets/)
            for (int i = 0; i < file.Length; i++) {
                file[i] = Assets.getRelativePath(file[i]);
            }
            musicFiles.AddRange(file);
        }

        if (musicFiles.Count == 0) {
            Log.warn("No music files found in music directory");
        } else {
            Log.info($"Found {musicFiles.Count} music track(s)");
        }
    }

    private void scheduleNextTrack() {
        // random delay between tracks
        nextTrackDelay = MIN_DELAY_SECONDS + random.NextDouble() * (MAX_DELAY_SECONDS - MIN_DELAY_SECONDS);
        timeSinceLastTrack = 0;
    }

    public void update(double dt) {
        if (snd.nosound || musicFiles.Count == 0) {
            return;
        }

        // if track is playing, don't start another
        if (trackPlaying) {
            return;
        }

        // accumulate time since last track
        timeSinceLastTrack += dt;

        // wait for delay, then play
        if (timeSinceLastTrack >= nextTrackDelay) {
            playRandomTrack();
            scheduleNextTrack();
        }
    }

    private void playRandomTrack() {
        if (musicFiles.Count == 0) {
            return;
        }

        var track = musicFiles[random.Next(musicFiles.Count)];

        var volume = 1.0f;
        foreach (var kv in volumes) {
            if (track.Contains(kv.Key)) {
                volume = kv.Value;
                break;
            }
        }
        currentTrack = snd.playMusic(track, volume);

        if (currentTrack != null) {
            currentTrack.loop = false;
            currentTrack.End += onTrackEnd;
            trackPlaying = true;

            Log.info($"Now playing: {Path.GetFileName(track)}");
        }
    }

    /** Called when track ends to allow next track to be scheduled */
    public void onTrackEnd() {
        trackPlaying = false;
        currentTrack = null;
    }
}
