using System;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine {

    public State CurState { get; private set; }
    public State PrevState { get; private set; }

    private List<State> states = new();
    private List<Transition> anyStateTransitions = new();

    private float timeWhenCurStateStarted;
    
    private State nextStateAfterDelay;
    private float nextStateAtTime;
    private float lastUpdateTime;
    private bool isPaused;

    public bool Transitioning => nextStateAfterDelay != null;
    public bool OnLastState => states.Count > 0 && CurState != null && CurState == states[^1];
    
    public State CreateState(Action update = null, Action fixedUpdate = null, Action lateUpdate = null, 
        Action enter = null, Action exit = null, Action whileExiting = null) 
    {
        State newState = new(update, fixedUpdate, lateUpdate, enter, exit, whileExiting);
        if (states.Count == 0) {
            SetStateImmediate(newState);
        }
        states.Add(newState);
        return newState;
    }

    public Transition FromAny(State state) {
        Transition transition = new(state);
        anyStateTransitions.Add(transition);
        return transition;
    }

    public void SetState(State state) {
        nextStateAtTime = 0f;
        nextStateAfterDelay = state;
    }

    public bool SetStateIfNotCurrent(State state) {
        if (CurState == state) return false;
        SetState(state);
        return true;
    }
    
    public void SetStateWithoutCallbacks(State state) {
        PrevState = CurState;
        CurState = state;
        nextStateAfterDelay = null;
        timeWhenCurStateStarted = Time.time;
    }

    public void StopCurrentTransition() {
        nextStateAfterDelay = null;
        timeWhenCurStateStarted = Time.time;
    }
    
    public int GetCurrentStateIndex() {
        if (states.Count <= 0) return -1;
        return states.IndexOf(CurState); 
    }
    
    public State StateFromIndex(int index) {
        if (states.Count <= 0) return null;
        return states[index]; 
    }
    
    public bool NotPassedThisState(State state) {
        return states.IndexOf(CurState) <= states.IndexOf(state);
    }
    
    public void Pause() {
        isPaused = true;
    }
    
    public void UnPause() {
        isPaused = false;
    }
    
    public enum UpdateMode { Update, FixedUpdate, LateUpdate }

    public void Tick(UpdateMode updateMode = UpdateMode.Update) {
        if (isPaused) return;
        
        bool needsUpdating = Time.time != lastUpdateTime;
        
        if (needsUpdating) {
            lastUpdateTime = Time.time;

            if (nextStateAfterDelay != null) {
                UpdateDelayedState();
                return;
            }
        
            UpdateState(anyStateTransitions);
            UpdateState(CurState.Transitions);
        } 
        
        switch (updateMode) {
            case UpdateMode.Update:
                CurState.OnStateUpdateAction?.Invoke();
                break;
            case UpdateMode.FixedUpdate:
                CurState.OnStateFixedUpdateAction?.Invoke();
                break;
            case UpdateMode.LateUpdate:
                CurState.OnStateLateUpdateAction?.Invoke();
                break;
        }
    }
    
    private void UpdateDelayedState() {
        if (Time.time < nextStateAtTime) {
            CurState.WhileExiting?.Invoke();
            return;
        }
        SetStateImmediate(nextStateAfterDelay);
    }

    private void UpdateState(List<Transition> transitions) {
        foreach (Transition transition in transitions) {
            float secondsInCurState = Time.time - timeWhenCurStateStarted;
            if (secondsInCurState >= transition.Seconds && transition.EvaluateTransition()) {
                SetStateWithDelay(transition.NextState, transition.Delay);
                break;
            }
        }
    }
    
    private void SetStateImmediate(State state) {
        PrevState = CurState;
        CurState = state;
        nextStateAfterDelay = null;
        PrevState?.OnStateExitAction?.Invoke();
        CurState.OnStateEnterAction?.Invoke();
        timeWhenCurStateStarted = Time.time;
    }

    private void SetStateWithDelay(State state, float delay) {
        if (delay <= 0f) {
            SetStateImmediate(state);
            return;
        }
        nextStateAtTime = Time.time + delay;
        nextStateAfterDelay = state;
    }
    
}