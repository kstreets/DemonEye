using System;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
[ExecuteAlways]
#endif
public class PixelFillManager : MonoBehaviour {
    
    public Image image;
    public Material pixelFillMaterial;
    public Texture fillMask;
    public Texture upwardsFillMask;
    
    private static readonly int fillId = Shader.PropertyToID("_Fill");
    private static readonly int offsetSizeId = Shader.PropertyToID("_Offset_Size");
    private static readonly int intoOffsetSizeId = Shader.PropertyToID("_IntoOffset_Size");
    private static readonly int fillMaskId = Shader.PropertyToID("_FillMask");
    private static readonly int useSmoothingId = Shader.PropertyToID("_UseSmoothing");
    
#if UNITY_EDITOR
    private void Update() {
        if (Application.isPlaying) return;
        
        if (image == null || pixelFillMaterial == null) return;
        
        if (image.material.shader != pixelFillMaterial.shader) {
            image.material = new(pixelFillMaterial);
        }
        
        SetMaterialFill(image.material.GetFloat(fillId));
    }
#endif
    
    public enum FillDirection { None, Up }
    
    public void Init(FillDirection fillDir) {
        image.material = new(pixelFillMaterial);
        image.material.SetTexture(fillMaskId, fillDir switch {
            FillDirection.None => fillMask,
            FillDirection.Up   => upwardsFillMask,
            _ => throw new ArgumentOutOfRangeException(nameof(fillDir), fillDir, null),
        });  
        SetMaterialFill(1f);
    }
    
    public void SetMaterialFill(float fill) {
        if (image.sprite == null) {
            image.material.SetFloat(fillId, fill);
            return;
        }
        
        Rect spriteRect = image.sprite.rect;
        Vector2 textureSize = new(image.mainTexture.width, image.mainTexture.height);
        image.material.SetVector(offsetSizeId, new(spriteRect.x / textureSize.x, spriteRect.y / textureSize.y, spriteRect.width / textureSize.x,  spriteRect.height / textureSize.y));
        image.material.SetFloat(fillId, fill);
    }
    
    public void SetIntoSprite(Sprite sprite) {
        if (image.sprite == null) {
            image.material.SetVector(intoOffsetSizeId, Vector4.zero);
            return;
        }
        
        Rect baseRect = image.sprite.rect;
        Rect spriteRect = sprite.textureRect;
        Vector2 textureSize = new(sprite.texture.width, sprite.texture.height);
        image.material.SetVector(intoOffsetSizeId, new((spriteRect.x - baseRect.x) / textureSize.x, (spriteRect.y - baseRect.y) / textureSize.y, 0f, 0f));
    }
    
    public void UseSmoothing(bool useSmoothing) {
        image.material.SetFloat(useSmoothingId, useSmoothing ? 1f : 0f);
    }
    
}
