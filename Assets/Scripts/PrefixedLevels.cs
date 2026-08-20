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
        return XpAlongCurrentLevel(xp) / (float)TotalXpAtCurrentLevel(xp);
    }
    
    public bool AnimateXpBarFill(ref int curXp, ref int gainedXp, out float xpBarFill) {
        if (gainedXp == 0) {
            xpBarFill = CurrentLevelCompletion(curXp);
            return false;
        } 
        
        int toGo = XpUntilNextLevel(curXp);

        if (gainedXp < toGo) {
            curXp += gainedXp;
            gainedXp = 0;
            xpBarFill = CurrentLevelCompletion(curXp);
            return true;
        }

        gainedXp -= toGo;
        curXp += toGo;
        xpBarFill = 1f;
        return true;
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

    public int XpAlongCurrentLevel(int curXp) {
        int prevLevel = GetLevelFromXp(curXp) - 1;
        return curXp - prefixedLevels[prevLevel];
    }
    
    public int XpUntilNextLevel(int curXp) {
        int curLevel = GetLevelFromXp(curXp);
        int xpLeftToGo = prefixedLevels[curLevel] - curXp; 
        return xpLeftToGo;
    }
    
    public bool ReachedMaxLevel(int xp) {
        return xp >= prefixedLevels[^1];
    }
    
}
