using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestGraphRuntime", menuName = "Scriptable Objects/QuestGraphRuntime")]
public class QuestGraphRuntime : ScriptableObject {

    [Serializable]
    public class Node {
        public Quest curQuest;
        public int saveIndex;
        [SerializeReference] // Don't serialize by value to prevent duplicates of same objects
        public List<Node> nextNodes;
    }

    public Node rootNode;
    public int questCount;
    
    public List<Quest> unorderedQuests;

    public void Build() {
        if (rootNode == null) return;

        HashSet<Node> visited = new();
        
        Queue<Node> queue = new();
        foreach (Node rootChild in rootNode.nextNodes) {
            queue.Enqueue(rootChild);
        }

        int curSaveIndex = 0;
        while (queue.Count > 0) {
            Node node = queue.Dequeue();
            
            if (!visited.Contains(node)) {
                node.saveIndex = curSaveIndex++;
                visited.Add(node);
            }
            
            if (node.nextNodes == null) continue;
            
            foreach (Node nextNode in node.nextNodes) {
                queue.Enqueue(nextNode);
            }
        }
        
        questCount = visited.Count;
        unorderedQuests = visited.ToList().Select(x => x.curQuest).ToList();
    }

}
