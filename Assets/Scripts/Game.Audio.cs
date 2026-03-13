using System.Collections.Generic;
using UnityEngine;

public partial class Game {
    
    private Dictionary<int, List<DynamicClipRecord>> clipRecords = new(50);
    private Queue<AudioSource> reservedAudioSources;
    
    private struct DynamicClipRecord {
        public float timePlayed;
        public Vector2 positionPlayed;
    }

    private void InitAudio() {
        const int numberOfSources = 20;
        reservedAudioSources = new(numberOfSources);
        
        for (int i = 0; i < numberOfSources; i++) {
            GameObject audioGo = Instantiate(dynamicAudioSourcePrefab, transform);
            reservedAudioSources.Enqueue(audioGo.GetComponent<AudioSource>());
        }
    }
    
    private void PlayAudioClip(DynamicClip dynamicClip, Vector2 position, float volumeScaler = 1f) {
        if (ClipIsViolatingLocalArea(dynamicClip, position)) return;
        
        AudioSource source = reservedAudioSources.Dequeue();
        reservedAudioSources.Enqueue(source);

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
        
        bool recordsExits = clipRecords.TryGetValue(clip.GetInstanceID(), out List<DynamicClipRecord> records);
        
        if (!recordsExits) {
            const int initCapacity = 10;
            List<DynamicClipRecord> newRecords = new(initCapacity);
            
            newRecords.Add(new() {  
                timePlayed = Time.time, 
                positionPlayed = clipPos 
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
    
    private AudioSource ambienceAudioSource;
    
    private void PlayAmbience() {
        ambienceAudioSource = reservedAudioSources.Dequeue();
        ambienceAudioSource.transform.position = Vector3.zero;
        ambienceAudioSource.volume = 1f;
        ambienceAudioSource.pitch = 1f;
        ambienceAudioSource.rolloffMode = AudioRolloffMode.Linear;
        ambienceAudioSource.minDistance = 500;
        ambienceAudioSource.maxDistance = 500;

        ambienceAudioSource.loop = true;
        ambienceAudioSource.clip = ambienceClip;
        ambienceAudioSource.outputAudioMixerGroup = ambienceMixerGroup;
        ambienceAudioSource.Play();
    }

    private void StopAmbience() {
        ambienceAudioSource.Stop();
        reservedAudioSources.Enqueue(ambienceAudioSource);
        ambienceAudioSource = null;
    }
    
}
