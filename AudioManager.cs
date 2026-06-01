using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace GAME;

public enum MusicKey
{
    Climb,
    Cruise,
}

public enum SoundKey
{
    PlanePassFar,
    PlanePassNear,
    TcasBeep,
    TerrainDoot,
    WarningBlip,
    ScorePling,
    SeatbeltDing,
}

public sealed class AudioManager : IDisposable
{
    private readonly Dictionary<MusicKey, string> _musicPaths = new()
    {
        [MusicKey.Climb] = "Assets/Audio/Music/climb_loop.ogg",
        [MusicKey.Cruise] = "Assets/Audio/Music/cruise_loop.ogg",
    };

    private readonly Dictionary<SoundKey, string> _sfxPaths = new()
    {
        [SoundKey.PlanePassFar] = "Assets/Audio/SFX/plane_pass_far.wav",
        [SoundKey.PlanePassNear] = "Assets/Audio/SFX/plane_pass_near.wav",
        [SoundKey.TcasBeep] = "Assets/Audio/SFX/tcas_beep.wav",
        [SoundKey.TerrainDoot] = "Assets/Audio/SFX/terrain_doot.wav",
        [SoundKey.WarningBlip] = "Assets/Audio/SFX/warning_blip.wav",
        [SoundKey.ScorePling] = "Assets/Audio/SFX/score_pling.wav",
        [SoundKey.SeatbeltDing] = "Assets/Audio/SFX/seatbelt_ding.wav",
    };

    private readonly Dictionary<MusicKey, Song> _music = new();
    private readonly Dictionary<SoundKey, SoundEffect> _sfx = new();
    private readonly Dictionary<SoundKey, float> _lastSfxTime = new();
    private readonly Dictionary<SoundKey, float> _cooldowns = new()
    {
        [SoundKey.PlanePassFar] = 1.2f,
        [SoundKey.PlanePassNear] = 1.0f,
        [SoundKey.TcasBeep] = 0.9f,
        [SoundKey.TerrainDoot] = 1.1f,
        [SoundKey.WarningBlip] = 0.7f,
        [SoundKey.ScorePling] = 0.25f,
        [SoundKey.SeatbeltDing] = 0.25f,
    };

    private float _time;
    private MusicKey? _currentMusic;

    public float MasterVolume { get; set; } = 0.75f;
    public float MusicVolume { get; set; } = 0.55f;
    public float SfxVolume { get; set; } = 0.85f;

    public void Load()
    {
        LoadMusicFiles();
        LoadSfxFiles();
        MediaPlayer.IsRepeating = true;
    }

    public void Update(GameTime gameTime, FlightPhase flightPhase)
    {
        _time += (float)gameTime.ElapsedGameTime.TotalSeconds;
        MediaPlayer.Volume = MathHelper.Clamp(MasterVolume * MusicVolume, 0f, 1f);

        var musicKey = GetMusicForPhase(flightPhase);
        if (musicKey.HasValue)
            PlayMusic(musicKey.Value);
    }

    public void PlaySfx(SoundKey key)
    {
        var cooldown = _cooldowns.TryGetValue(key, out var customCooldown) ? customCooldown : 0.5f;
        var lastTime = _lastSfxTime.TryGetValue(key, out var savedTime) ? savedTime : -999f;

        if (_time - lastTime < cooldown)
            return;

        if (!_sfx.TryGetValue(key, out var sound))
            return;

        _lastSfxTime[key] = _time;
        sound.Play(MathHelper.Clamp(MasterVolume * SfxVolume, 0f, 1f), 0f, 0f);
    }

    public void Dispose()
    {
        foreach (var sound in _sfx.Values)
            sound.Dispose();

        _sfx.Clear();
        _music.Clear();
    }

    private void PlayMusic(MusicKey key)
    {
        if (_currentMusic == key)
            return;

        if (!_music.TryGetValue(key, out var song))
            return;

        _currentMusic = key;
        MediaPlayer.Play(song);
    }

    private void LoadMusicFiles()
    {
        foreach (var pair in _musicPaths)
        {
            var fullPath = Path.GetFullPath(pair.Value);
            if (!File.Exists(fullPath))
            {
                LogMissing(pair.Value);
                continue;
            }

            try
            {
                _music[pair.Key] = Song.FromUri(pair.Key.ToString(), new Uri(fullPath));
            }
            catch (Exception ex)
            {
                LogLoadError(pair.Value, ex);
            }
        }
    }

    private void LoadSfxFiles()
    {
        foreach (var pair in _sfxPaths)
        {
            var fullPath = Path.GetFullPath(pair.Value);
            if (!File.Exists(fullPath))
            {
                LogMissing(pair.Value);
                continue;
            }

            try
            {
                using var stream = File.OpenRead(fullPath);
                _sfx[pair.Key] = SoundEffect.FromStream(stream);
            }
            catch (Exception ex)
            {
                LogLoadError(pair.Value, ex);
            }
        }
    }

    private static MusicKey? GetMusicForPhase(FlightPhase flightPhase)
    {
        return flightPhase switch
        {
            FlightPhase.Takeoff or FlightPhase.InitialClimb => MusicKey.Climb,
            FlightPhase.Cruise or FlightPhase.Descent => MusicKey.Cruise,
            _ => null,
        };
    }

    private static void LogMissing(string path)
    {
        Debug.WriteLine($"Ses dosyası bulunamadı: {path}");
        Console.WriteLine($"Ses dosyası bulunamadı: {path}");
    }

    private static void LogLoadError(string path, Exception ex)
    {
        Debug.WriteLine($"Ses dosyası yüklenemedi: {path} - {ex.Message}");
        Console.WriteLine($"Ses dosyası yüklenemedi: {path} - {ex.Message}");
    }
}
