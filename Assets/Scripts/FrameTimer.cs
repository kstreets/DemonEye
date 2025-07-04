using System;
using UnityEngine;

public struct FrameTimer {

    public int FramesLeft { get; private set; }
    public Action EndAction;
    public Action UpdateAction;

    private int startFrameCount;

    public bool IsFinished => FramesLeft <= 0; 
    
    public bool Tick() {
        // Don't want to subtract on the same frame this gets set
        if (startFrameCount == Time.frameCount) return false;
        
        if (FramesLeft <= 0) return true;

        FramesLeft -= 1;
        UpdateAction?.Invoke();
        
        if (FramesLeft > 0) return false;
        
        EndAction?.Invoke();
        return true;
    }

    public void SetFrames(int frames) {
        FramesLeft = frames;
        startFrameCount = Time.frameCount;
    }
    
    public void Stop() {
        FramesLeft = 0;
    }

}