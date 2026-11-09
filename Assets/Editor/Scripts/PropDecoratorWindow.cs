using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// Scatters small props around hand placed big props, using the rules in a PropDecorationRules asset
public class PropDecoratorWindow : EditorWindow {

    private const string rulesPrefKey = "PropDecorator.Rules";
    private const string decorationsName = "Decorations";
    private const int attemptsPerPiece = 15;

    private PropDecorationRules rules;

    private struct Occupied {
        public Vector2 center;
        public float radius;
    }

    [MenuItem("Tools/Prop Decorator")]
    private static void Open() {
        GetWindow<PropDecoratorWindow>("Prop Decorator");
    }

    private void OnEnable() {
        rules = AssetDatabase.LoadAssetAtPath<PropDecorationRules>(EditorPrefs.GetString(rulesPrefKey));
    }

    private void OnSelectionChange() {
        Repaint();
    }

    private void OnGUI() {
        EditorGUI.BeginChangeCheck();
        rules = (PropDecorationRules)EditorGUILayout.ObjectField("Rules", rules, typeof(PropDecorationRules), false);
        if (EditorGUI.EndChangeCheck()) {
            EditorPrefs.SetString(rulesPrefKey, AssetDatabase.GetAssetPath(rules));
        }
        if (rules == null) return;

        List<GameObject> anchors = FindSelectedAnchors();
        EditorGUILayout.HelpBox($"Select big props, or the object holding them. {anchors.Count} selected props have a rule.\n" +
                                "Decorating again replaces a prop's decorations.", MessageType.Info);

        using (new EditorGUI.DisabledScope(anchors.Count == 0)) {
            if (GUILayout.Button("Decorate")) {
                Decorate(anchors);
            }
            if (GUILayout.Button("Clear")) {
                Clear(anchors);
            }
        }
    }

    // A selected object without a rule counts as a parent, so selecting Props decorates everything in it
    private List<GameObject> FindSelectedAnchors() {
        List<GameObject> anchors = new();
        foreach (GameObject selected in Selection.gameObjects) {
            if (FindRule(selected) != null) {
                anchors.Add(selected);
                continue;
            }
            foreach (Transform child in selected.transform) {
                if (FindRule(child.gameObject) != null) {
                    anchors.Add(child.gameObject);
                }
            }
        }
        return anchors;
    }

    private PropDecorationRules.Rule FindRule(GameObject instance) {
        if (!PrefabUtility.IsAnyPrefabInstanceRoot(instance)) return null;
        // Nearest prefab first, then the original in case it's a variant
        GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(instance);
        return rules.FindRule(source) ?? rules.FindRule(PrefabUtility.GetCorrespondingObjectFromOriginalSource(instance));
    }

    private void Clear(List<GameObject> anchors) {
        Undo.SetCurrentGroupName("Clear Prop Decorations");
        int group = Undo.GetCurrentGroup();
        foreach (GameObject anchor in anchors) {
            RemoveDecorations(anchor);
        }
        Undo.CollapseUndoOperations(group);
    }

    private static void RemoveDecorations(GameObject anchor) {
        for (int i = anchor.transform.childCount - 1; i >= 0; i--) {
            Transform child = anchor.transform.GetChild(i);
            if (child.name == decorationsName) {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }
    }

    private void Decorate(List<GameObject> anchors) {
        Undo.SetCurrentGroupName("Decorate Props");
        int group = Undo.GetCurrentGroup();

        foreach (GameObject anchor in anchors) {
            RemoveDecorations(anchor);
        }
        Physics2D.SyncTransforms();

        foreach (GameObject anchor in anchors) {
            DecorateAnchor(anchor, FindRule(anchor), FindOccupied(anchor));
        }
        Undo.CollapseUndoOperations(group);
    }

    // Other props and their decorations, so pieces don't pile onto neighbours
    private static List<Occupied> FindOccupied(GameObject anchor) {
        List<Occupied> occupied = new();
        Transform props = anchor.transform.parent;
        if (props == null) return occupied;

        foreach (SpriteRenderer sprite in props.GetComponentsInChildren<SpriteRenderer>()) {
            if (sprite.transform.IsChildOf(anchor.transform)) continue;
            if (sprite.sprite == null) continue;
            bool isDecoration = sprite.transform.parent != null && sprite.transform.parent.name == decorationsName;
            Bounds bounds = WorldBounds(sprite);
            occupied.Add(new Occupied {
                center = bounds.center,
                // Big props only claim their base, decorations their whole footprint
                radius = isDecoration ? Mathf.Max(bounds.extents.x, bounds.extents.y) : bounds.extents.x * 0.5f,
            });
        }
        return occupied;
    }

    private void DecorateAnchor(GameObject anchor, PropDecorationRules.Rule rule, List<Occupied> occupied) {
        if (rule.decorations == null || rule.decorations.Length == 0) return;

        SpriteRenderer anchorSprite = anchor.GetComponentInChildren<SpriteRenderer>();
        if (anchorSprite == null || anchorSprite.sprite == null) return;

        // Pieces go around the base of the prop, which is wider than it is deep from the top down
        float radiusX = WorldBounds(anchorSprite).extents.x;
        float radiusY = radiusX * 0.5f;
        Vector2 basePos = anchor.transform.position;

        MapInstance map = anchor.GetComponentInParent<MapInstance>();
        Tilemap floor = map != null && map.mainTilemapRenderer != null ? map.mainTilemapRenderer.GetComponent<Tilemap>() : null;
        List<Tilemap> collisionTilemaps = new();
        if (map != null) {
            foreach (Tilemap tilemap in map.GetComponentsInChildren<Tilemap>()) {
                if (tilemap.name.Contains("Collision")) {
                    collisionTilemaps.Add(tilemap);
                }
            }
        }

        GameObject parent = new(decorationsName);
        Undo.RegisterCreatedObjectUndo(parent, "Decorate Props");
        parent.transform.SetParent(anchor.transform, false);

        int target = Random.Range(rule.count.x, rule.count.y + 1);
        float facing = Random.value * Mathf.PI * 2f;
        float spread = Mathf.Lerp(Mathf.PI, Mathf.PI * 0.3f, rule.sideBias);
        int placed = 0;

        for (int attempt = 0; attempt < target * attemptsPerPiece && placed < target; attempt++) {
            GameObject prefab = PickDecoration(rule);
            SpriteRenderer prefabSprite = prefab != null ? prefab.GetComponentInChildren<SpriteRenderer>() : null;
            if (prefabSprite == null || prefabSprite.sprite == null) continue;

            float angle = facing + Random.Range(-spread, spread);
            // Squared so pieces bunch up near the prop and thin out further away
            float t = Random.value;
            float distance = Mathf.Lerp(rule.distance.x, rule.distance.y, t * t);
            Vector2 pos = basePos + new Vector2(Mathf.Cos(angle) * (radiusX + distance), Mathf.Sin(angle) * (radiusY + distance));

            bool flip = rule.randomFlip && Random.value < 0.5f;
            Bounds local = prefabSprite.sprite.bounds;
            Vector2 offset = local.center;
            if (flip) {
                offset.x = -offset.x;
            }
            Vector2 center = pos + offset;
            Vector2 extents = local.extents;
            float radius = Mathf.Max(extents.x, extents.y);

            if (!IsOnFloor(center, extents, floor, collisionTilemaps)) continue;
            if (Overlaps(center, radius, occupied, rules.spacing)) continue;

            // Nothing sits inside a collider, and solid pieces keep clear of other colliders entirely
            bool solid = HasSolidCollider(prefab);
            if (HitsSolid(Physics2D.OverlapPointAll(center))) continue;
            if (solid && HitsSolid(Physics2D.OverlapCircleAll(center, radius))) continue;

            GameObject piece = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
            Undo.RegisterCreatedObjectUndo(piece, "Decorate Props");
            piece.transform.position = new Vector3(pos.x, pos.y, anchor.transform.position.z);
            if (flip) {
                piece.GetComponentInChildren<SpriteRenderer>().flipX = true;
            }
            if (solid) {
                Physics2D.SyncTransforms();
            }

            occupied.Add(new Occupied { center = center, radius = radius });
            placed++;
        }
    }

    // Renderer bounds of objects created this frame are stale, so work it out from the sprite and transform
    private static Bounds WorldBounds(SpriteRenderer renderer) {
        Bounds local = renderer.sprite.bounds;
        Vector3 center = local.center;
        if (renderer.flipX) {
            center.x = -center.x;
        }
        Vector3 scale = renderer.transform.lossyScale;
        center = Vector3.Scale(center, scale);
        Vector3 extents = Vector3.Scale(local.extents, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), 1f));
        return new Bounds(renderer.transform.position + center, extents * 2f);
    }

    private static GameObject PickDecoration(PropDecorationRules.Rule rule) {
        float total = 0f;
        foreach (PropDecorationRules.Decoration decoration in rule.decorations) {
            total += Mathf.Max(0f, decoration.weight);
        }
        float pick = Random.value * total;
        foreach (PropDecorationRules.Decoration decoration in rule.decorations) {
            pick -= Mathf.Max(0f, decoration.weight);
            if (pick <= 0f) return decoration.prefab;
        }
        return rule.decorations[^1].prefab;
    }

    // The whole footprint has to be on walkable ground, so nothing hangs over the edge of the map
    private static bool IsOnFloor(Vector2 center, Vector2 extents, Tilemap floor, List<Tilemap> collisionTilemaps) {
        if (floor == null) return true;
        Vector2[] points = {
            center,
            center + new Vector2(-extents.x, -extents.y),
            center + new Vector2(extents.x, -extents.y),
            center + new Vector2(-extents.x, extents.y),
            center + new Vector2(extents.x, extents.y),
        };
        foreach (Vector2 point in points) {
            if (!floor.HasTile(floor.WorldToCell(point))) return false;
            foreach (Tilemap collision in collisionTilemaps) {
                if (collision.HasTile(collision.WorldToCell(point))) return false;
            }
        }
        return true;
    }

    private static bool Overlaps(Vector2 center, float radius, List<Occupied> occupied, float spacing) {
        foreach (Occupied other in occupied) {
            float minDistance = radius + other.radius + spacing;
            if ((other.center - center).sqrMagnitude < minDistance * minDistance) return true;
        }
        return false;
    }

    // Triggers like pickups don't count
    private static bool HitsSolid(Collider2D[] hits) {
        foreach (Collider2D hit in hits) {
            if (!hit.isTrigger) return true;
        }
        return false;
    }

    private static bool HasSolidCollider(GameObject prefab) {
        foreach (Collider2D collider in prefab.GetComponentsInChildren<Collider2D>(true)) {
            if (!collider.isTrigger) return true;
        }
        return false;
    }

}
