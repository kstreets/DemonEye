using System.Collections.Generic;
using UnityEngine;
using VInspector;
using static Game;

[CreateAssetMenu(fileName = "NewQuest", menuName = "Scriptable Objects/NewQuest")]
public class NewQuest : UuidScriptableObject {
    
    [Header("Info")]
    public string title;
    [TextArea(minLines: 6, maxLines: 10)] 
    public string description;
    
    [Header("Rewards")]
    public int traderReputationReward;
    
    public List<ObjectiveData> objectives;
}
