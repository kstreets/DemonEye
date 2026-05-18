using System.Collections.Generic;
using UnityEngine;

public partial class Game {
    
    public struct DynamicClipRecord {
        public float timePlayed;
        public Vector2 positionPlayed;
    }
    
    private void InitAudio() {
        const int numberOfSources = 20;
        gameData.audio.reservedSources = new(numberOfSources);
        for (int i = 0; i < numberOfSources; i++) {
            GameObject audioGo = Instantiate(gameData.prefabs.audioSource, transform);
            gameData.audio.reservedSources.Enqueue(audioGo.GetComponent<AudioSource>());
        }
    }
    
    private void PlayAudioClip(DynamicClip dynamicClip, Vector2 position, float volumeScaler = 1f) {
        if (ClipIsViolatingLocalArea(dynamicClip, position)) return;
        
        AudioSource source = gameData.audio.reservedSources.Dequeue();
        gameData.audio.reservedSources.Enqueue(source);

        float distFromPlayer = Vector2.Distance(player.position, position);
        float volumeLerp = distFromPlayer / dynamicClip.maxDistance;
        float volume = Mathf.Lerp(1f, 0f, volumeLerp) * volumeScaler;
        
        source.transform.position = position;
        source.rolloffMode = dynamicClip.rolloffMode;
        source.clip = dynamicClip.clips[Random.Range(0, dynamicClip.clips.Length)];
        source.outputAudioMixerGroup = dynamicClip.mixerGroup;
        source.volume = volume;
        source.pitch = Random.Range(dynamicClip.minPitch, dynamicClip.maxPitch);
        source.minDistance = dynamicClip.minDistance;
        source.maxDistance = dynamicClip.maxDistance;
        source.loop = false;
        source.Play();
    }

    private bool ClipIsViolatingLocalArea(DynamicClip clip, Vector2 clipPos) {
        if (clip.localAreaCooldownTime <= 0f || clip.localAreaDistance <= 0f) {
            return false;
        }
        
        var clipRecords = gameData.audio.records;
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
    
    private void PlayAmbience() {
        var ambience = gameData.audio.ambienceSource;
        
        ambience = gameData.audio.reservedSources.Dequeue();
        ambience.transform.position = Vector3.zero;
        ambience.volume = 1f;
        ambience.pitch = 1f;
        ambience.rolloffMode = AudioRolloffMode.Linear;
        ambience.minDistance = 500;
        ambience.maxDistance = 500;

        ambience.loop = true;
        ambience.clip = ambienceClip;
        ambience.outputAudioMixerGroup = ambienceMixerGroup;
        ambience.Play();
    }

    private void StopAmbience() {
        var ambience = gameData.audio.ambienceSource;
        ambience.Stop();
        gameData.audio.reservedSources.Enqueue(ambience);
        ambience = null;
    }
    
}
