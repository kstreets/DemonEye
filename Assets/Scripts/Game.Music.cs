using System;
using PrimeTween;
using UnityEngine;

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
        
        bool forceSwitch = loopsDone >= 2 || (loopsDone >= 1 && spawnManager.waveStartedThisFrame);
        if (forceSwitch && PlayingMusic && !MusicIsTransitioning && !FadingMusicOut) {
            music.lastGameplaySong = music.source.clip;
            TransitionToSong(music.gameplayMusic.GetRandom(exclude: music.lastGameplaySong), MusicOption.Fast);
        }
        
        if (spawnManager.waveStartedThisFrame && !PlayingMusic) {
            PlayMusic(music.gameplayMusic.GetRandom(exclude: music.lastGameplaySong), MusicOption.Smooth);
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
    
    public enum MusicOption { Hard, Fast, Medium, Smooth }
    
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
