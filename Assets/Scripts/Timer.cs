using System;
using UnityEngine;

public struct Timer {

    public float CurTime { get; private set; }
    public Action EndAction;
    public Action UpdateAction;

    private float startTime;

    public bool IsFinished => CurTime <= 0f;
    
    public bool Tick() {
        if (CurTime <= 0f) return true;

        CurTime -= Time.deltaTime;
        UpdateAction?.Invoke();
        
        if (CurTime > 0f) return false;
        
        EndAction?.Invoke();
        return true;
    }

    public void SetTime(float newTime) {
        CurTime = newTime;
        startTime = newTime;
    }
    
    public void Stop() {
        CurTime = 0f;
    }

    public float Comp() {
        if (startTime == 0f) return 0f;
        return Mathf.Clamp(1f - (CurTime / startTime), 0f, 1f);
    }
    
    public float InvComp() {
        if (startTime == 0f) return 0f;
        return Mathf.Clamp(CurTime / startTime, 0f, 1f);
    }

    public float TimePassed() {
        if (startTime == 0f) return 0f;
        return startTime - CurTime;
    }

}