using System;
using System.Collections;
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
    
    public override void OnImportAsset(AssetImportContext ctx) {
        QuestGraph graph = GraphDatabase.LoadGraphForImporter<QuestGraph>(ctx.assetPath);
        if (graph == null) return;

        QuestStartNode entryNode = graph.GetNodes().OfType<QuestStartNode>().FirstOrDefault();
        if (entryNode == null) return;

        QuestGraphRuntime runtimeAsset = ScriptableObject.CreateInstance<QuestGraphRuntime>();
        QuestGraphRuntime.Node rootNode = TraverseGraph(entryNode); 
        runtimeAsset.rootNode = rootNode;
        runtimeAsset.Build();
        
        ctx.AddObjectToAsset("Runtime", runtimeAsset);
        ctx.SetMainObject(runtimeAsset);
    }

    private QuestGraphRuntime.Node TraverseGraph(INode curNode) {
        QuestGraphRuntime.Node runtimeNode = new();
        if (TryGetOption<Quest>(curNode, out Quest quest)) {
            runtimeNode.curQuest = quest;
        }
        
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
