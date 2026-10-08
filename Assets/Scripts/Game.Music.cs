using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.Pool;
using static GameData;

public partial class Game {
    
    private bool PlayingMusic => music.source.isPlaying;
    private bool FadingMusicOut => music.fadingOutTween.isAlive;
    private bool MusicIsTransitioning => music.songTransition.tween.isAlive;
    private bool GameplayHasLowPassFilterOn => music.gameplaySnapshotWeights[1] > 0f;
    
    private void InitMusic() {
        GameObject audioGo = Instantiate(prefabs.audioSource, transform);
        music.source = audioGo.GetComponent<AudioSource>();
        music.source.outputAudioMixerGroup = music.masterGroup;
        music.source.loop = true;
        music.source.playOnAwake = false;
        music.source.spatialBlend = 0f;
        music.source.minDistance = 10000f;
        
        music.gameplayLowpassSnapshots = new[] { music.defaultSnapshot, music.lowPassSnapshot };
        music.gameplaySnapshotWeights = new float[2];
    }
    
    private void UpdateGameplayMusic() {
        if (curRaid.state == RaidState.PostFinalWave) {
            if (curRaid.stateSwitchedThisFrame) {
                StopMusic(MusicOption.Smooth);
            }
            return;
        }
        
        bool inBetweenWaves = spawnManager.FinishedSpawningThisWave && entities.enemies.Count <= 0;
        
        int loopsDone = 0;
        if (PlayingMusic && !MusicIsTransitioning && !FadingMusicOut) {
            loopsDone = Mathf.FloorToInt((Time.time - music.timeCurSongStarted) / music.source.clip.length); 
        }
        
        bool shouldFadeOutTrack = loopsDone >= 2 && inBetweenWaves; 
        if (shouldFadeOutTrack && !MusicIsTransitioning && !FadingMusicOut) {
            music.lastGameplaySong = music.source.clip;
            StopMusic(MusicOption.Smooth);
        }
        
        MusicIntensity intensity = CurWaveMusicIntensity;

        bool forceSwitch = loopsDone >= 2 || (loopsDone >= 1 && spawnManager.waveStartedThisFrame);
        if (forceSwitch && PlayingMusic && !MusicIsTransitioning && !FadingMusicOut) {
            music.lastGameplaySong = music.source.clip;
            TransitionToSong(PickGameplaySong(intensity, exclude: music.lastGameplaySong), MusicOption.Fast);
        }

        // A new wave can call for a different intensity than the song that's playing
        if (spawnManager.waveStartedThisFrame) {
            if (MusicIsTransitioning) {
                // The transition reads this once it finishes fading out, so we can just swap what it's going to play
                if (!SongHasIntensity(music.songTransition.song, intensity)) {
                    music.songTransition.song = PickGameplaySong(intensity, exclude: music.lastGameplaySong);
                }
            }
            else if (FadingMusicOut) {
                // The last song was fading out between waves. The source still counts as playing until the fade ends,
                // so nothing else would start music and the whole wave would be silent.
                music.fadingOutTween.Stop(); // So the transition fades out from the current volume instead of cutting to silence
                TransitionToSong(PickGameplaySong(intensity, exclude: music.lastGameplaySong), MusicOption.Smooth);
            }
            else if (PlayingMusic && !FadingMusicOut && !SongHasIntensity(music.source.clip, intensity)) {
                music.lastGameplaySong = music.source.clip;
                TransitionToSong(PickGameplaySong(intensity, exclude: music.lastGameplaySong), MusicOption.Fast);
            }
        }

        if (spawnManager.waveStartedThisFrame && !PlayingMusic) {
            PlayMusic(PickGameplaySong(intensity, exclude: music.lastGameplaySong), MusicOption.Smooth);
        }
        
        const float lowPassTransitionDuration = 0.5f;
        if (PlayerInventoryIsOpen && !GameplayHasLowPassFilterOn) {
            music.gameplaySnapshotWeights[0] = 0f;
            music.gameplaySnapshotWeights[1] = 1f;
            music.masterGroup.audioMixer.TransitionToSnapshots(music.gameplayLowpassSnapshots, music.gameplaySnapshotWeights, lowPassTransitionDuration);
        }
        else if (!PlayerInventoryIsOpen && GameplayHasLowPassFilterOn) {
            music.gameplaySnapshotWeights[0] = 1f;
            music.gameplaySnapshotWeights[1] = 0f;
            music.masterGroup.audioMixer.TransitionToSnapshots(music.gameplayLowpassSnapshots, music.gameplaySnapshotWeights, lowPassTransitionDuration);
        }
    }
    
    // The menu song takes breaks so it doesn't get repetitive while sitting in the menus
    private const int menuMinLoopsBeforeBreak = 2;
    private const int menuMaxLoopsBeforeBreak = 3;
    private const float menuMinBreakDuration = 60f;
    private const float menuMaxBreakDuration = 100f;

    // Covers every menu (main menu, hideout, map selection, settings) since the menu song keeps playing between them
    private void UpdateMenuMusic() {
        if (!music.menuMusicActive) return;
        if (MusicIsTransitioning || FadingMusicOut) return;

        if (PlayingMusic) {
            if (music.source.clip != music.mainMenuMusic) return;

            // Time the fade out to finish right as the last loop ends, instead of fading over the song restarting
            float fadeDuration = GetMusicFadeSpeed(MusicOption.Smooth);
            float breakStartTime = music.timeCurSongStarted + music.menuLoopsBeforeBreak * music.source.clip.length - fadeDuration;
            if (Time.time >= breakStartTime) {
                StopMusic(MusicOption.Smooth);
                music.menuBreakEndTime = Time.time + fadeDuration + UnityEngine.Random.Range(menuMinBreakDuration, menuMaxBreakDuration);
            }
        }
        else if (Time.time >= music.menuBreakEndTime) {
            StartMenuMusic(MusicOption.Smooth);
        }
    }

    private void OnMenuMusicEnter() {
        music.menuMusicActive = true;

        // Coming back from a raid gets a moment of quiet before the menu music fades in
        State prevState = states.gameStateMachine.PrevState;
        // The first state is entered while the states are still being created, so they can all be null at that point
        bool returningFromRaid = prevState != null && (prevState == states.gameOver || prevState == states.winExit || prevState == states.earlyExit);
        if (returningFromRaid) {
            const float raidExitMusicDelay = 3f;
            music.menuBreakEndTime = Time.time + raidExitMusicDelay;
            return;
        }

        bool onBreak = Time.time < music.menuBreakEndTime;
        if (onBreak) return;

        bool alreadyPlaying = PlayingMusic && !FadingMusicOut && music.source.clip == music.mainMenuMusic;
        if (alreadyPlaying) return;

        StartMenuMusic(MusicOption.Fast);
    }

    private void OnMenuMusicExit() {
        music.menuMusicActive = false;
        music.menuBreakEndTime = 0f; // A break shouldn't carry over to the next time we're back in the menus
    }

    private void StartMenuMusic(MusicOption option) {
        music.menuLoopsBeforeBreak = UnityEngine.Random.Range(menuMinLoopsBeforeBreak, menuMaxLoopsBeforeBreak + 1);
        PlayMusic(music.mainMenuMusic, option);
    }

    public enum MusicOption { Hard, Fast, Medium, Smooth }

    // How fast and aggressive a song is
    public enum MusicIntensity { Low, Medium, High, Insane }

    private MusicIntensity CurWaveMusicIntensity => spawnManager.CurPhasePool?.musicIntensity ?? MusicIntensity.Low;

    private static bool SongHasIntensity(GameplaySong song, MusicIntensity intensity) {
        if (song.intensities == null || song.intensities.Count <= 0) {
            return intensity == MusicIntensity.Low;
        }
        return song.intensities.Contains(intensity);
    }

    private bool SongHasIntensity(AudioClip clip, MusicIntensity intensity) {
        foreach (GameplaySong song in music.gameplaySongs) {
            if (song.clip == clip) return SongHasIntensity(song, intensity);
        }
        return false;
    }

    private AudioClip PickGameplaySong(MusicIntensity intensity, AudioClip exclude) {
        using var _ = ListPool<AudioClip>.Get(out var options);
        foreach (GameplaySong song in music.gameplaySongs) {
            if (SongHasIntensity(song, intensity)) {
                options.Add(song.clip);
            }
        }

        if (options.Count <= 0) {
            Debug.LogWarning($"No gameplay songs have {intensity} intensity, picking from all of them instead");
            foreach (GameplaySong song in music.gameplaySongs) {
                options.Add(song.clip);
            }
        }

        // Only avoid repeating the last song when there's something else to play
        if (options.Count > 1) {
            options.Remove(exclude);
        }
        return options[UnityEngine.Random.Range(0, options.Count)];
    }
    
    private void PlayMusic(AudioClip song, MusicOption option) {
        if (PlayingMusic && music.source.clip == song) return;
        
        music.source.clip = song;
        music.source.outputAudioMixerGroup = song == music.mainMenuMusic ? music.mainMenuGroup : music.gameplayGroup;
        music.timeCurSongStarted = Time.time;
        music.songTransition.tween.Stop();
        FadeInMusic(GetMusicFadeSpeed(option));
    }
    
    private void StopMusic(MusicOption option) {
        if (!PlayingMusic) return;
        music.songTransition.tween.Stop();
        FadeOutMusic(GetMusicFadeSpeed(option));
    }
    
    public struct SongTransition {
        public AudioClip song; 
        public MusicOption option;
        public Tween tween;
    }
    
    private void TransitionToSong(AudioClip song, MusicOption option) {
        if (MusicIsTransitioning) return;
        
        if (!PlayingMusic) {
            PlayMusic(song, option);
            return;
        }
        
        float fadeoutDuration = GetMusicFadeSpeed(MusicOption.Fast);
        FadeOutMusic(fadeoutDuration);
        
        music.songTransition.song = song;
        music.songTransition.option = option;
        music.songTransition.tween = Tween.Delay(fadeoutDuration, static () => {
            SongTransition transition = gameInstance.music.songTransition;
            gameInstance.PlayMusic(transition.song, transition.option);
        });
    }
    
    private float GetMusicFadeSpeed(MusicOption option) {
        return option switch {
            MusicOption.Hard => 0f,
            MusicOption.Fast => 0.35f,
            MusicOption.Medium => 2f,
            MusicOption.Smooth => 10f,
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };
    }
    
    private void FadeOutMusic(float duration) {
        music.fadingInTween.Complete();
        music.fadingOutTween.Complete();
        
        music.fadingOutTween = Tween.AudioVolume(music.source, music.source.volume, 0f, duration);
        music.fadingOutTween.OnComplete(music.source, static (source) => {
            source.Stop();
            source.clip = null;
        });
    }
    
    private void FadeInMusic(float duration) {
        music.fadingInTween.Complete();
        music.fadingOutTween.Complete();
        
        music.source.Play();
        music.fadingInTween = Tween.AudioVolume(music.source, music.source.volume, 1f, duration);
    }
    
}
