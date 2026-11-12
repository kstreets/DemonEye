using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BubbleSpawner : MonoBehaviour {

    public GameObject bubblePrefab;
    public DynamicClip bubbleClip;
    public Color tint = Color.white;
    public List<Transform> spawnPoints;
    public bool playOnEnable;
    public bool loop;
    public int bubbleCount = 10;
    public float startDelay;
    public float interval = 0.35f;
    [Range(0f, 1f)] public float intervalJitter;
    
    [Tooltip("Evaluated Per Sequence, 0 keeps constant rate")]
    public AnimationCurve rateCurve;

    [NonSerialized] private readonly List<float> spawnTimes = new();
    [NonSerialized] private int nextSpawn;
    [NonSerialized] private int nextPoint = int.MaxValue;
    [NonSerialized] private float startTime;
    [NonSerialized] private bool playing;

    private void OnEnable() {
        if (playOnEnable) {
            Play();
        }
    }

    private void OnDisable() {
        Stop();
    }

    // Returns the time until the last bubble spawns
    public float Play() {
        return Schedule(startDelay);
    }

    public void Stop() {
        playing = false;
    }

    private float Schedule(float time) {
        spawnTimes.Clear();
        for (int i = 0; i < bubbleCount; i++) {
            bool useCurve = rateCurve != null && rateCurve.length > 0;
            float acc = useCurve ? rateCurve.Evaluate(i / (float)bubbleCount) : 0f;
            time += interval * (1f - acc) * (1f + Random.Range(-intervalJitter, intervalJitter));
            spawnTimes.Add(time);
        }
        nextSpawn = 0;
        startTime = Time.time;
        playing = spawnTimes.Count > 0;
        return time;
    }

    private void Update() {
        if (!playing) return;

        float elapsed = Time.time - startTime;
        while (nextSpawn < spawnTimes.Count && elapsed >= spawnTimes[nextSpawn]) {
            nextSpawn++;
            Game.gameInstance.SpawnBubble(this, NextSpawnPoint());
        }

        if (nextSpawn >= spawnTimes.Count) {
            if (loop) Schedule(0f);
            else playing = false;
        }
    }

    private Transform NextSpawnPoint() {
        if (!spawnPoints.IndexInRange(nextPoint)) {
            nextPoint = 0;
            spawnPoints.Shuffle();
        }
        return spawnPoints[nextPoint++];
    }

}
