using UnityEngine;

public struct Limitter {

    private float limitTime;
    private float lastTime;

    public Limitter(float time) {
        limitTime = time;
        lastTime = Mathf.NegativeInfinity;
    }

    public void ClearTime() {
        lastTime = 0f;
    }

    public bool TimeHasPassed() {
        if (Time.time - lastTime < limitTime) return false;
        lastTime = Time.time;
        return true;
    }
    
    public bool TimeHasPassed(float time) {
        if (Time.time - lastTime < time) return false;
        lastTime = Time.time;
        return true;
    }

}
