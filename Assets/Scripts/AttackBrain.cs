using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

public class AttackBrain<T> where T : Enum {
    
    private int curAttackLevel; // 0 - N where N is most powerful attacks
    private float[] curAttackProbs;
    private float[] defaultAttackProbs;
    private float[] repeatedProbDecays;
    
    private List<int> startingLevelPointers = new();
    private int curLevelLength;
    
    public AttackBrain(int attackCount) {
        curAttackProbs = new float[attackCount];
        defaultAttackProbs = new float[attackCount];
        repeatedProbDecays = new float[attackCount];
    }
    
    public void AddAttack(T attackType, int level, float defaultProb, float repeatedProbDecay) {
        Assert.IsTrue(level <= startingLevelPointers.Count, "Levels need to added in order and cannot be skipped");
        
        bool movedToNextLevel = !startingLevelPointers.IndexInRange(level);
        if (movedToNextLevel) {
            int startOfNextLevel = startingLevelPointers.Count == 0 ? 0 : startingLevelPointers[^1] + curLevelLength;
            startingLevelPointers.Add(startOfNextLevel);
            curLevelLength = 0;
        }
        curLevelLength++;
        
        int attackIndex = Convert.ToInt32(attackType);
        curAttackProbs[attackIndex] = defaultProb;
        defaultAttackProbs[attackIndex] = defaultProb;
        repeatedProbDecays[attackIndex] = repeatedProbDecay;
    }
    
    public T ChooseAttack(int level) {
        Assert.IsTrue(startingLevelPointers.IndexInRange(level), $"Attack level {level} is out of range, [0..{startingLevelPointers.Count})");
        
        int startingIndex = startingLevelPointers[level];
        
        int length = 0;
        if (startingLevelPointers.IndexInRange(level + 1)) {
            length = startingLevelPointers[level + 1] - startingIndex;
        }
        else {
            length = defaultAttackProbs.Length - startingIndex;
        }
        
        using var _ = ListPool<float>.Get(out var attackChances);
        float totalAttackChances = 0f;
        for (int i = startingIndex; i < startingIndex + length; i++) {
            attackChances.Add(curAttackProbs[i]);
            totalAttackChances += curAttackProbs[i];
        }
        
        int chosenAttackIndex = startingIndex;
        float roll = Random.value * totalAttackChances;
        float threshold = 0f;
        for (int i = 0; i < length; i++) {
            float chance = attackChances[i];
            threshold += chance;
            if (roll < threshold) {
                chosenAttackIndex = startingIndex + i;
                break;
            }
        }
        
        float newAttackProb = curAttackProbs[chosenAttackIndex];
        curAttackProbs[chosenAttackIndex] = Mathf.Clamp01(newAttackProb - repeatedProbDecays[chosenAttackIndex]);

        for (int i = 0; i < curAttackProbs.Length; i++) {
            if (i == chosenAttackIndex) continue;
            curAttackProbs[i] = defaultAttackProbs[i];
        }

        return Unsafe.As<int, T>(ref chosenAttackIndex);
    }
    
}
