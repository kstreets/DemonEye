using System;
using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "KillQuest", menuName = "Scriptable Objects/KillQuest")]
public class KillQuest : Quest {

    public EnemyData targetEnemy;
    public int killCountRequirement;
    [NonSerialized] public int curKillCount;

    protected override void OnInit() {
        onEnemyDeath += OnEnemyDeath;
    }

    public override void OnComplete() {
        onEnemyDeath -= OnEnemyDeath;
    }
    
    protected override void OnLoadSaveState(SaveState saveState) {
        curKillCount = saveState.objectiveCounts[0];
    }

    protected override void OnWriteToSaveState(SaveState saveState) {
        saveState.objectiveCounts.Add(curKillCount);
    }

    private void OnEnemyDeath(Enemy enemy) {
        if (targetEnemy == null) {
            curKillCount++;
        }
        else if (targetEnemy == enemy.data) {
            curKillCount++;
        }
        canCompleteQuestFlag = curKillCount >= killCountRequirement;
    }
    
}
