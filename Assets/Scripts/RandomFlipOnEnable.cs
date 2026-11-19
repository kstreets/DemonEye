using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RandomFlipOnEnable : MonoBehaviour {

    [Range(0f, 1f)] public float flipXChance = 0.5f;
    [Range(0f, 1f)] public float flipYChance;
    SpriteRenderer spriteRenderer;

    void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable() {
        spriteRenderer.flipX = Random.value < flipXChance;
        spriteRenderer.flipY = Random.value < flipYChance;
    }

}
