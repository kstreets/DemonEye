using System;
using UnityEngine;

// Which small props get scattered around which big props, used by the Prop Decorator window
[CreateAssetMenu(fileName = "PropDecorationRules", menuName = "Scriptable Objects/Prop Decoration Rules")]
public class PropDecorationRules : ScriptableObject {

    [Serializable]
    public class Decoration {
        public GameObject prefab;
        public float weight = 1f;
    }

    [Serializable]
    public class Rule {
        public string name;
        public GameObject[] anchors;
        public Decoration[] decorations;
        public Vector2Int count = new(4, 8);
        [Tooltip("Distance out from the edge of the big prop")]
        public Vector2 distance = new(0.02f, 0.45f);
        [Tooltip("0 spreads pieces all the way around, 1 keeps them on one side")]
        [Range(0f, 1f)] public float sideBias = 0.5f;
        public bool randomFlip = true;
    }

    public Rule[] rules;
    [Tooltip("Gap kept between decorations")]
    public float spacing = 0.03f;

    public Rule FindRule(GameObject prefab) {
        foreach (Rule rule in rules) {
            if (Array.IndexOf(rule.anchors, prefab) >= 0) return rule;
        }
        return null;
    }

}
