using UnityEngine;

[CreateAssetMenu(fileName = "Trader", menuName = "Scriptable Objects/Trader")]
public class Trader : ScriptableObject {

    public PrefixedLevels levels;
    public State state;
    
    public class State {
        public int reputation;
        public int raidsUntilRestock;
    }

    public int GetLevel() => levels.GetLevelFromXp(state.reputation);
    public float CurrentLevelCompletion() => levels.CurrentLevelCompletion(state.reputation);
    public int LevelsGainedFromXp(int xpToGain) => levels.LevelsGainedFromXp(state.reputation, xpToGain);
    public int TotalXpAtCurrentLevel() => levels.TotalXpAtCurrentLevel(state.reputation);
    public int XpUntilNextLevel() => levels.XpUntilNextLevel(state.reputation);
    public bool ReachedMaxLevel() => levels.ReachedMaxLevel(state.reputation);

}
