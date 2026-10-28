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

    protected State() { }

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

    public virtual void InvokeUpdate() => OnStateUpdateAction?.Invoke();
    public virtual void InvokeFixedUpdate() => OnStateFixedUpdateAction?.Invoke();
    public virtual void InvokeLateUpdate() => OnStateLateUpdateAction?.Invoke();
    public virtual void InvokeEnter() => OnStateEnterAction?.Invoke();
    public virtual void InvokeExit() => OnStateExitAction?.Invoke();
    public virtual void InvokeWhileExiting() => WhileExiting?.Invoke();

}

public class State<T> : State {

    public readonly T Target;

    public Action<T> OnStateUpdateTargetAction;
    public Action<T> OnStateFixedUpdateTargetAction;
    public Action<T> OnStateLateUpdateTargetAction;
    public Action<T> OnStateEnterTargetAction;
    public Action<T> OnStateExitTargetAction;
    public Action<T> WhileExitingTarget;

    public State(T target, Action<T> update, Action<T> fixedUpdate, Action<T> lateUpdate, Action<T> enter, Action<T> exit, Action<T> whileExiting) {
        Target = target;
        OnStateUpdateTargetAction = update;
        OnStateFixedUpdateTargetAction = fixedUpdate;
        OnStateLateUpdateTargetAction = lateUpdate;
        OnStateEnterTargetAction = enter;
        OnStateExitTargetAction = exit;
        WhileExitingTarget = whileExiting;
    }

    public override void InvokeUpdate() => OnStateUpdateTargetAction?.Invoke(Target);
    public override void InvokeFixedUpdate() => OnStateFixedUpdateTargetAction?.Invoke(Target);
    public override void InvokeLateUpdate() => OnStateLateUpdateTargetAction?.Invoke(Target);
    public override void InvokeEnter() => OnStateEnterTargetAction?.Invoke(Target);
    public override void InvokeExit() => OnStateExitTargetAction?.Invoke(Target);
    public override void InvokeWhileExiting() => WhileExitingTarget?.Invoke(Target);

}
