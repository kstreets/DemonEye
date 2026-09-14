using UnityEngine;
using UnityEngine.UI;
using VInspector;

public class ShaderOffsetAndSizeHelper : MonoBehaviour {
    
    public Material targetMaterial;
    public Image sourceImage;
    public SpriteRenderer spriteRenderer;
    
    [Button]
    private void SetShaderOffsetAndSize() {
        Vector4 offsetAndSize = sourceImage != null ? sourceImage.OffsetAndSizeInTexture() : spriteRenderer.sprite.OffsetAndSizeInTexture();
        if (targetMaterial != null) {
            targetMaterial.SetVector("_Offset_Size", offsetAndSize);
            return;
        }
        Material mat = sourceImage != null ? sourceImage.material : spriteRenderer.sharedMaterial;
        mat.SetVector("_Offset_Size", offsetAndSize);
    }
    
    
}
