using UnityEngine;

public struct Duration {
    
    private float startTime;
    private float duration;
    
    public void Add(float time) {
        if (IsAlive()) {
            duration += time;
            return;
        }
        Reset(time);
    }
    
    public void Reset(float time) {
        duration = time;
        startTime = Time.time;
    }
    
    public bool TryReset(float time) {
        if (IsAlive()) {
            return false;
        }
        Reset(time);
        return true;
    }
    
    public bool HasPassed() {
        return Time.time > startTime + duration;
    }
    
    public bool IsAlive() {
        return !HasPassed();
    }
    
}
