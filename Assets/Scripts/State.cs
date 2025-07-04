using System;
using System.Collections.Generic;

public class State {

    public readonly List<Transition> Transitions = new();

    public Action OnStateUpdateAction;
    public Action OnStateEnterAction;
    public Action OnStateExitAction;
    public Action WhileExiting;

    public State(Action update, Action enter, Action exit, Action whileExiting = null) {
        OnStateUpdateAction = update;
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