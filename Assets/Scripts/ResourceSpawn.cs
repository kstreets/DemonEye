using System;
using System.Collections.Generic;
using UnityEngine;
using VInspector;
using Random = UnityEngine.Random;

[ExecuteInEditMode]
public class ResourceSpawn : MonoBehaviour {

    [Serializable]
    public class Element {
        public GameObject prefab;
        public float spawnChance;
    }
    
    public List<Element> elements;

    public bool blockedByExitPortal;
    [ShowIf("blockedByExitPortal")]
    public Portal blockingPortal;
    [EndIf]

    public bool IsBlockedByExitPortal => blockedByExitPortal && blockingPortal != null && blockingPortal.gameObject.activeSelf;

    public float TotalSpawnChance {
        get {
            float total = 0f;
            foreach (Element element in elements) {
                total += element.spawnChance;
            }
            return total;
        }
    }

    public enum GroupRoll { None, Picked, NotPicked }
    [NonSerialized] public GroupRoll groupRoll;

    public GameObject GetPrefabToSpawn() {
        if (groupRoll == GroupRoll.NotPicked) return null;

        float total = TotalSpawnChance;
        // A picked spawn already passed its chance to spawn in the group roll, so it only chooses which prefab
        if (groupRoll != GroupRoll.Picked) {
            total = total < 1f ? 1f : total;
        }
        
        float prefixedSum = 0f;
        float random = Random.Range(0f, total);
        foreach (Element element in elements) {
            prefixedSum += element.spawnChance;
            if (random < prefixedSum) {
                return element.prefab;
            }
        }
        
        return null;
    }
    
#if UNITY_EDITOR
    
    private static Material outlineMaterial;
    
    private void Update() {
        if (!TryGetComponent(out SpriteRenderer spriteRenderer)) {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        if (elements == null || elements.Count <= 0) return;
        
        var prefabSpriteRenderer = elements[0].prefab?.GetComponent<SpriteRenderer>();
        if (prefabSpriteRenderer == null) return;
        
        if (outlineMaterial == null) {
            outlineMaterial = new(Shader.Find("Shader Graphs/OutlineShader"));
            outlineMaterial.SetColor("_Color", Color.coral);
        }
        
        spriteRenderer.color = new(1f, 1f, 1f, 0.4f);
        spriteRenderer.material = outlineMaterial;
        spriteRenderer.sprite = prefabSpriteRenderer.sprite;
        spriteRenderer.sortingLayerID = prefabSpriteRenderer.sortingLayerID;
        spriteRenderer.sortingOrder = prefabSpriteRenderer.sortingOrder;
    }

    private void OnValidate() {
        if (blockedByExitPortal && blockingPortal == null) {
            LinkClosestExitPortal();
        }
    }

    [ContextMenu("Link Closest Exit Portal")]
    private void LinkClosestExitPortal() {
        MapInstance mapInstance = GetComponentInParent<MapInstance>();
        if (mapInstance == null || mapInstance.exitPortalsParent == null) return;

        float closestDistSqr = float.MaxValue;
        foreach (Transform portalTrans in mapInstance.exitPortalsParent) {
            float distSqr = Vector2.SqrMagnitude(portalTrans.position - transform.position);
            if (distSqr < closestDistSqr && portalTrans.TryGetComponent(out Portal portal)) {
                closestDistSqr = distSqr;
                blockingPortal = portal;
            }
        }
    }

    private void OnDrawGizmos() {
        if (!blockedByExitPortal || blockingPortal == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, blockingPortal.transform.position);
        Gizmos.DrawWireSphere(blockingPortal.transform.position, 0.15f);
    }

#endif

}