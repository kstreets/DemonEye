using System;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
[ExecuteAlways]
#endif
public class ForgeEffect : MonoBehaviour {
    
    public Image image;
    public Material pixelFillMaterial;
    public Texture fillMask;
    public Texture upwardsFillMask;
    
#if UNITY_EDITOR
    private void Update() {
        if (Application.isPlaying) return;
        
        if (image == null || pixelFillMaterial == null) return;
        
        if (image.material.shader != pixelFillMaterial.shader) {
            image.material = new(pixelFillMaterial);
        }
        
        SetMaterialFill(image.material.GetFloat(ShaderIds.fill));
    }
#endif
    
    public enum FillDirection { None, Up }
    
    public void Init(FillDirection fillDir) {
        image.material = new(pixelFillMaterial);
        image.material.SetTexture(ShaderIds.fillMask, fillDir switch {
            FillDirection.None => fillMask,
            FillDirection.Up   => upwardsFillMask,
            _ => throw new ArgumentOutOfRangeException(nameof(fillDir), fillDir, null),
        });  
        SetMaterialFill(1f);
        SetActive(false);
    }
    
    public void SetActive(bool active) {
        image.material.SetInt(ShaderIds.active, active ? 1 : 0);
    }
    
    public void SetMaterialFill(float fill) {
        if (image.sprite == null) {
            image.material.SetFloat(ShaderIds.fill, fill);
            return;
        }
        
        Rect spriteRect = image.sprite.rect;
        Vector2 textureSize = new(image.mainTexture.width, image.mainTexture.height);
        image.material.SetVector(ShaderIds.offsetSize, new(spriteRect.x / textureSize.x, spriteRect.y / textureSize.y, spriteRect.width / textureSize.x,  spriteRect.height / textureSize.y));
        image.material.SetFloat(ShaderIds.fill, fill);
    }
    
    public void SetIntoSprite(Sprite sprite) {
        if (image.sprite == null) {
            image.material.SetVector(ShaderIds.intoOffsetSize, Vector4.zero);
            return;
        }
        
        Rect baseRect = image.sprite.rect;
        Rect spriteRect = sprite.textureRect;
        Vector2 textureSize = new(sprite.texture.width, sprite.texture.height);
        image.material.SetVector(ShaderIds.intoOffsetSize, new((spriteRect.x - baseRect.x) / textureSize.x, (spriteRect.y - baseRect.y) / textureSize.y, 0f, 0f));
    }
    
    public void UseSmoothing(bool useSmoothing) {
        image.material.SetFloat(ShaderIds.useSmoothing, useSmoothing ? 1f : 0f);
    }
    
}
