using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEditor.AssetImporters;

[Graph(assetExtension)]
[Serializable]
public class QuestGraph : Graph {
    
    public const string assetExtension = "questgraph";

    [MenuItem("Assets/Create/Quest Graph", false)]
    public static void CreateAssetFile() {
        GraphDatabase.PromptInProjectBrowserToCreateNewAsset<QuestGraph>();     
    }

    public override void OnGraphChanged(GraphLogger graphLogger) {
        List<QuestStartNode> startNodes = new();
        Dictionary<Quest, int> questCounter = new();
        
        foreach (INode node in GetNodes()) {
            if (node is QuestStartNode) {
                startNodes.Add((QuestStartNode)node);
            }
            if (node is QuestGraphNode questNode) {
                if (!questNode.GetNodeOption(0).TryGetValue(out Quest quest)) continue;
                if (!quest) {
                    graphLogger.LogWarning("Missing quest", node);
                    continue;
                }
                if (questCounter.ContainsKey(quest)) {
                    questCounter[quest]++;
                    continue;
                }
                questCounter.Add(quest, 1);
            }
        }

        if (startNodes.Count > 1) {
            graphLogger.LogError("Should not have more than 1 start node");
        }
        
        foreach ((Quest quest, int count) in questCounter) {
            if (count == 1) continue;
            graphLogger.LogError($"Quest '{quest.name}' is referenced by ({count}) different nodes, it should just be 1");
        }
    }
    
}

public class QuestStartNode : Node {
    
    protected override void OnDefinePorts(IPortDefinitionContext context) {
        context.AddOutputPort("Output").Build();
    }

}

public class QuestGraphNode : Node {
    
    protected override void OnDefinePorts(IPortDefinitionContext context) {
        context.AddInputPort("Input").Build();
        context.AddOutputPort("Output").Build();
    }

    protected override void OnDefineOptions(IOptionDefinitionContext context) {
        context.AddOption<Quest>("Quest Reference");
    }
    
}

[ScriptedImporter(1, QuestGraph.assetExtension)]
public class QuestGraphImporter : ScriptedImporter {
    
    private Dictionary<INode, QuestGraphRuntime.Node> existingNodesDictionary = new();
    
    public override void OnImportAsset(AssetImportContext ctx) {
        QuestGraph graph = GraphDatabase.LoadGraphForImporter<QuestGraph>(ctx.assetPath);
        if (graph == null) return;

        QuestStartNode entryNode = graph.GetNodes().OfType<QuestStartNode>().FirstOrDefault();
        if (entryNode == null) return;
        
        existingNodesDictionary.Clear();

        QuestGraphRuntime runtimeAsset = ScriptableObject.CreateInstance<QuestGraphRuntime>();
        QuestGraphRuntime.Node rootNode = TraverseGraph(entryNode); 
        runtimeAsset.rootNode = rootNode;
        runtimeAsset.Build();
        
        ctx.AddObjectToAsset("Runtime", runtimeAsset);
        ctx.SetMainObject(runtimeAsset);
    }

    private QuestGraphRuntime.Node TraverseGraph(INode curNode) {
        if (existingNodesDictionary.TryGetValue(curNode, out QuestGraphRuntime.Node existingNode)) {
            return existingNode;
        }
        
        QuestGraphRuntime.Node runtimeNode = new();
        runtimeNode.curQuest = TryGetOption(curNode, out Quest quest) ? quest : null;
        existingNodesDictionary.Add(curNode, runtimeNode);
        
        List<IPort> connectedPorts = new();
        curNode.GetOutputPort(0).GetConnectedPorts(connectedPorts);
        
        if (connectedPorts.Count > 0) {
            runtimeNode.nextNodes = new();
        }
        
        foreach (IPort connectedPort in connectedPorts) {
            INode connectedNode = connectedPort.GetNode();
            runtimeNode.nextNodes.Add(TraverseGraph(connectedNode));
        }
        
        return runtimeNode;
    }

    private bool TryGetOption<T>(INode iNode, out T type) {
        Node node = iNode as Node;
        if (node.nodeOptionCount <= 0) {
            type = default;
            return false;
        }
        return node.GetNodeOption(0).TryGetValue(out type) && type != null;
    }
    
}
