using System;

public class Transition {
    
    public State NextState { get; set; }
    public float Seconds { get; private set; }
    public float Delay { get; private set; }
    private Func<bool> condition;

    public Transition(State nextState) {
        this.NextState = nextState;
    }
    
    public bool EvaluateTransition() {
        if (condition == null) return true;
        return condition.Invoke();
    }
    
    public Transition When(Func<bool> condition) {
        this.condition = condition;
        return this;
    }
    
    public Transition AfterSeconds(float seconds) {
        Seconds = seconds;
        return this;
    }
    
    public void WithDelay(float seconds) {
        Delay = seconds;
    }
    
}