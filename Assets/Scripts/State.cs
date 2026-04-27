using System;
using System.Collections.Generic;

public class State {

    public readonly List<Transition> Transitions = new();

    public Action OnStateUpdateAction;
    public Action OnStateFixedUpdateAction;
    public Action OnStateLateUpdateAction;
    public Action OnStateEnterAction;
    public Action OnStateExitAction;
    public Action WhileExiting;

    public State(Action update, Action fixedUpdate, Action lateUpdate, Action enter, Action exit, Action whileExiting) {
        OnStateUpdateAction = update;
        OnStateFixedUpdateAction = fixedUpdate;
        OnStateLateUpdateAction = lateUpdate;
        OnStateEnterAction = enter;
        OnStateExitAction = exit;
        WhileExiting = whileExiting;
    }
    
    public Transition To(State state) {
        Transition transition = new(state);
        Transitions.Add(transition);
        return transition;
    }

}