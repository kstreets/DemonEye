using System;
using Febucci.Numbers;

[Serializable]
public class PrefixedLevels {
    
    public int[] prefixedLevels;
    
    public int GetLevelFromXp(int xp) {
        for (int i = 0; i < prefixedLevels.Length; i++) {
            if (xp < prefixedLevels[i]) {
                return i;
            }
        }
        return prefixedLevels.Length;
    }
    
    public float CurrentLevelCompletion(int xp) {
        if (ReachedMaxLevel(xp)) {
            return 1f;
        }
        return XpUntilNextLevel(xp) / (float)TotalXpAtCurrentLevel(xp);
    }
    
    public float AnimateXpBarFill(ref int curXp, ref int gainedXp) {
        int toGo = XpUntilNextLevel(curXp);
        int totalForLevel = TotalXpAtCurrentLevel(curXp);
        
        gainedXp -= toGo;
        if (gainedXp < 0) {
            gainedXp = 0;
        }
        
        
        if (gainedXp < toGo) {
            gainedXp = 0;
            return 0f;
        }
        return 0f;
    }
    
    public int LevelsGainedFromXp(int curXp, int xpToGain) {
        if (xpToGain < 0) return 0;
        return GetLevelFromXp(curXp + xpToGain) - GetLevelFromXp(curXp);
    }
    
    public int TotalXpAtCurrentLevel(int curXp) {
        int curLevel = GetLevelFromXp(curXp);
        int sumAtCurLevel = prefixedLevels[curLevel]; 
        int sumAtPrevLevel = prefixedLevels[curLevel - 1];
        return sumAtCurLevel - sumAtPrevLevel;
    }
    
    public int XpUntilNextLevel(int curXp) {
        int curLevel = GetLevelFromXp(curXp);
        int sumAtPrevLevel = prefixedLevels[curLevel - 1];
        int xpLeftToGo = sumAtPrevLevel - curXp; 
        return xpLeftToGo;
    }
    
    public bool ReachedMaxLevel(int xp) {
        return xp >= prefixedLevels[^1];
    }
    
}
