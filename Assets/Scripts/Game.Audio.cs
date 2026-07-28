using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class Game {
    
    public struct DynamicClipRecord {
        public float timePlayed;
        public Vector2 positionPlayed;
    }
    
    public struct AudioClipHandle : IEquatable<AudioClipHandle> {
        public AudioSource audioSource;
        public int generation;
        
        public bool Equals(AudioClipHandle other) => audioSource == other.audioSource && generation == other.generation;
        public override bool Equals(object obj) => obj is AudioClipHandle other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(audioSource, generation);
    }
    
    private void InitAudio() {
        const int numberOfSources = 25;
        audio.reservedSources = new(numberOfSources);
        for (int i = 0; i < numberOfSources; i++) {
            GameObject audioGo = Instantiate(prefabs.audioSource, transform);
            AudioSource source = audioGo.GetComponent<AudioSource>();
            audio.reservedSources.Enqueue(source);
            audio.generationLookup.Add(source, 0);
        }
    }
    
    private AudioClipHandle PlayAudioClip(DynamicClip dynamicClip, Vector2 position, float volumeScaler = 1f, float pitch = 0f, bool loop = false) {
        if (ClipIsViolatingLocalArea(dynamicClip, position)) {
            return new();
        }
        
        AudioSource source = audio.reservedSources.Dequeue();
        int nextGeneration = audio.generationLookup[source] + 1;
        audio.generationLookup[source] = nextGeneration;
        
        AudioClipHandle handle = new() {
            audioSource = source,
            generation = nextGeneration,
        };
        
        if (loop) { 
            audio.loopingSources.Add(handle);    
        }
        else {
            audio.reservedSources.Enqueue(source);
        }

        float distFromPlayer = Vector2.Distance(player.position, position);
        float volumeLerp = distFromPlayer / dynamicClip.maxDistance;
        float volume = Mathf.Lerp(1f, 0f, volumeLerp) * volumeScaler;
        
        source.transform.position = position;
        source.rolloffMode = dynamicClip.rolloffMode;
        source.clip = dynamicClip.clips[Random.Range(0, dynamicClip.clips.Length)];
        source.outputAudioMixerGroup = dynamicClip.mixerGroup;
        source.volume = volume;
        source.pitch = pitch == 0f ? Random.Range(dynamicClip.minPitch, dynamicClip.maxPitch) : pitch;
        source.minDistance = dynamicClip.minDistance;
        source.maxDistance = dynamicClip.maxDistance;
        source.loop = loop;
        source.Play();
        
        return handle;
    }
    
    private void StopAudioClip(AudioClipHandle handle) {
        int curAudioSourceGen = audio.generationLookup[handle.audioSource];
        bool handleIsValid = handle.generation == curAudioSourceGen;
        if (!handleIsValid) return;
        
        handle.audioSource.Stop();
        audio.generationLookup[handle.audioSource] = curAudioSourceGen + 1;
        
        // If the clip is non-looping then its already be in the reserved queue
        if (!audio.reservedSources.Contains(handle.audioSource)) {
            audio.reservedSources.Enqueue(handle.audioSource);
        }
        
        audio.loopingSources.Remove(handle);
    }
    
    private void StopAllAudioClips() {
        foreach (AudioSource reservedSource in audio.reservedSources) {
            reservedSource.Stop();
        }
        for (int i = audio.loopingSources.Count - 1; i >= 0; i--) {
            StopAudioClip(audio.loopingSources[i]);
        }
    }
    
    private bool ClipIsViolatingLocalArea(DynamicClip clip, Vector2 clipPos) {
        if (clip.localAreaCooldownTime <= 0f || clip.localAreaDistance <= 0f) {
            return false;
        }
        
        var clipRecords = audio.records;
        bool recordsExits = clipRecords.TryGetValue(clip.GetInstanceID(), out List<DynamicClipRecord> records);
        
        if (!recordsExits) {
            const int initCapacity = 10;
            List<DynamicClipRecord> newRecords = new(initCapacity);
            
            newRecords.Add(new() {  
                timePlayed = Time.time, 
                positionPlayed = clipPos, 
            });
            
            clipRecords.Add(clip.GetInstanceID(), newRecords);
            return false;
        }
        
        float cooldownTime = clip.localAreaCooldownTime;
        float areaDistance = clip.localAreaDistance;
        
        // Remove any records that have been expired
        for (int i = records.Count - 1; i >= 0; i--) {
            bool recordHadExpired = Time.time >= records[i].timePlayed + cooldownTime;
            if (recordHadExpired) {
                records.RemoveAt(i);         
            }
        }
        
        // After removing expired records, check to see if one is too close to the potential pos
        foreach (DynamicClipRecord record in records) {
            if (Vector3.Distance(record.positionPlayed, clipPos) < areaDistance) {
                return true;
            } 
        }
        
        // Add a new record since we are going to play the sound
        records.Add(new() {  
            timePlayed = Time.time, 
            positionPlayed = clipPos,
        });

        return false;
    }
    
    private static void GetRarityVolumeAndPitch(Item.Rarity rarity, out float volume, out float pitch) {
        switch (rarity) {
            case Item.Rarity.Common:
                volume = 0.25f; pitch = 1.5f;
                break;
            case Item.Rarity.Uncommon:
                volume = 0.5f; pitch = 1.25f;
                break;
            case Item.Rarity.Rare:
                volume = 0.75f; pitch = 1.1f;
                break;
            case Item.Rarity.Epic:
                volume = 1f; pitch = 0.9f;
                break;
            case Item.Rarity.Legendary:
                volume = 1f; pitch = 0.75f;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    
}
