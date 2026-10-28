using UnityEngine;

// A cooldown that gets longer when it's used again right after it's ready, so spamming an action is punished.
// Waiting a little after it's ready resets the delay back to its base.
public class EscalatingCooldown {

    public float baseDelay;
    public float maxDelay;
    public float increasePerRapidUse;
    // How soon after the cooldown is ready a use has to happen to count as rapid
    public float rapidUseWindow;

    private float curDelay;
    private float lastUseTime = Mathf.NegativeInfinity;

    public EscalatingCooldown(float baseDelay, float maxDelay, float increasePerRapidUse, float rapidUseWindow) {
        this.baseDelay = baseDelay;
        this.maxDelay = maxDelay;
        this.increasePerRapidUse = increasePerRapidUse;
        this.rapidUseWindow = rapidUseWindow;
        curDelay = baseDelay;
    }

    public float CurrentDelay => curDelay;
    public bool IsReady => Time.time - lastUseTime >= curDelay;
    public float RemainingTime => Mathf.Max(0f, curDelay - (Time.time - lastUseTime));

    public bool TryUse() {
        float timeSinceLastUse = Time.time - lastUseTime;
        if (timeSinceLastUse < curDelay) return false;

        float timeSinceReady = timeSinceLastUse - curDelay;
        bool rapidUse = timeSinceReady <= rapidUseWindow;
        curDelay = rapidUse ? Mathf.Min(curDelay + increasePerRapidUse, maxDelay) : baseDelay;

        lastUseTime = Time.time;
        return true;
    }

    public void Reset() {
        curDelay = baseDelay;
        lastUseTime = Mathf.NegativeInfinity;
    }

}
