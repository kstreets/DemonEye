using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

// Each level is a bag of marbles. Drawing removes a marble; the bag refills once empty.
public class AttackBrain<T> where T : Enum {

    private int[] marbleCounts;
    private int[] marblesLeft;
    private bool[] preventConsecutives;
    private int lastChosenIndex = -1;

    private List<int> startingLevelPointers = new();
    private int curLevelLength;

    public AttackBrain(int attackCount) {
        marbleCounts = new int[attackCount];
        marblesLeft = new int[attackCount];
        preventConsecutives = new bool[attackCount];
    }

    public void AddAttack(T attackType, int level, int marbles, bool preventConsecutive = false) {
        Assert.IsTrue(level <= startingLevelPointers.Count, "Levels need to added in order and cannot be skipped");
        Assert.IsTrue(marbles > 0, "Attacks need at least one marble");

        bool movedToNextLevel = !startingLevelPointers.IndexInRange(level);
        if (movedToNextLevel) {
            int startOfNextLevel = startingLevelPointers.Count == 0 ? 0 : startingLevelPointers[^1] + curLevelLength;
            startingLevelPointers.Add(startOfNextLevel);
            curLevelLength = 0;
        }
        curLevelLength++;

        int attackIndex = Convert.ToInt32(attackType);
        marbleCounts[attackIndex] = marbles;
        marblesLeft[attackIndex] = marbles;
        this.preventConsecutives[attackIndex] = preventConsecutive;
    }

    public T ChooseAttack(int level) {
        Assert.IsTrue(startingLevelPointers.IndexInRange(level), $"Attack level {level} is out of range, [0..{startingLevelPointers.Count})");

        int start = startingLevelPointers[level];
        int end = startingLevelPointers.IndexInRange(level + 1) ? startingLevelPointers[level + 1] : marbleCounts.Length;

        int blockedIndex = lastChosenIndex >= 0 && preventConsecutives[lastChosenIndex] ? lastChosenIndex : -1;

        // Only blocked marbles left, refill early
        int totalLeft = CountDrawable(start, end, blockedIndex);
        if (totalLeft == 0) {
            for (int i = start; i < end; i++) {
                marblesLeft[i] = marbleCounts[i];
            }
            totalLeft = CountDrawable(start, end, blockedIndex);
        }

        // Level only has the blocked attack
        if (totalLeft == 0) {
            blockedIndex = -1;
            totalLeft = CountDrawable(start, end, blockedIndex);
        }

        int roll = Random.Range(0, totalLeft);
        int chosenAttackIndex = start;
        for (int i = start; i < end; i++) {
            if (i == blockedIndex) continue;
            roll -= marblesLeft[i];
            if (roll < 0) {
                chosenAttackIndex = i;
                break;
            }
        }

        marblesLeft[chosenAttackIndex]--;
        lastChosenIndex = chosenAttackIndex;
        return Unsafe.As<int, T>(ref chosenAttackIndex);
    }

    private int CountDrawable(int start, int end, int blockedIndex) {
        int total = 0;
        for (int i = start; i < end; i++) {
            if (i == blockedIndex) continue;
            total += marblesLeft[i];
        }
        return total;
    }

}
