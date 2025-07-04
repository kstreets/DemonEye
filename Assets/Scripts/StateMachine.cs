using System;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine {

    public State CurState { get; private set; }
    public State PrevState { get; private set; }

    private List<State> states = new();
    private List<Transition> anyStateTransitions = new();

    private float timeSinceStateStart;
    
    private State nextStateAfterDelay;
    private float nextStateDelay;
    private float curNextStateDelay;

    public bool Transitioning => nextStateAfterDelay != null;
    
    public State CreateState(Action update, Action enter, Action exit, Action whileExiting = null) {
        State newState = new(update, enter, exit, whileExiting);
        if (states.Count == 0) {
            CurState = newState;
            PrevState = newState;
            CurState.OnStateEnterAction?.Invoke();
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
        nextStateDelay = 0f;
        nextStateAfterDelay = state;
    }

    public bool SetStateIfNotCurrent(State state) {
        if (CurState == state) return false;
        nextStateDelay = 0f;
        nextStateAfterDelay = state;
        return true;
    }

    public void StopCurrentTransition() {
        nextStateAfterDelay = null;
        timeSinceStateStart = 0f;
        curNextStateDelay = 0f;
    }
    
    public void Tick() {
        timeSinceStateStart += Time.deltaTime;

        if (nextStateAfterDelay != null) {
            UpdateDelayedState();
            return;
        }
        
        UpdateState(anyStateTransitions);
        UpdateState(CurState.Transitions);
        CurState.OnStateUpdateAction?.Invoke();
    }

    private void UpdateDelayedState() {
        curNextStateDelay += Time.deltaTime;
        if (curNextStateDelay < nextStateDelay) {
            CurState.WhileExiting?.Invoke();
            return;
        }
        SetStateImediate(nextStateAfterDelay);
    }

    private void UpdateState(List<Transition> transitions) {
        foreach (Transition transition in transitions) {
            if (timeSinceStateStart >= transition.Seconds && transition.EvaluateTransition()) {
                SetStateImediate(transition.NextState, transition.Delay);
                break;
            }
        }
    }
    
    private void SetStateImediate(State state) {
        PrevState = CurState;
        CurState = state;
        nextStateAfterDelay = null;
        PrevState.OnStateExitAction?.Invoke();
        CurState.OnStateEnterAction?.Invoke();
        timeSinceStateStart = 0f;
        curNextStateDelay = 0f;
    }

    private void SetStateImediate(State state, float delay) {
        if (delay <= 0f) {
            SetStateImediate(state);
            return;
        }

        nextStateDelay = delay;
        nextStateAfterDelay = state;
    }
    
}