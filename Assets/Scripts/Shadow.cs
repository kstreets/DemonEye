using System;
using UnityEngine;

public class Shadow : MonoBehaviour {

    public SpriteRenderer spriteRenderer;
    [NonSerialized] public float currentRadius = 0.05f;

    private MaterialPropertyBlock propertyBlock;
    
    private void Awake() {
        propertyBlock = new();
        SetRadius(0f);
    }

    private void OnDisable() {
        SetRadius(0f);
    }

    public void SetRadius(float radius) {
        if (currentRadius == radius) return;
        
        currentRadius = radius;
        float spriteWorldWidth = spriteRenderer.sprite.bounds.size.x;
        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(ShaderIds.radius, radius / spriteWorldWidth);
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }

}
