using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestLine", menuName = "Scriptable Objects/QuestLine")]
public class QuestLine : ScriptableObject {

    public List<Quest> quests;

}
