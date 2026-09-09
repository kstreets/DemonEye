using UnityEngine;
using VInspector;

public class ShaderOffsetAndSizeHelper : MonoBehaviour {
    
    public SpriteRenderer spriteRenderer;
    
    [Button]
    private void SetShaderOffsetAndSize() {
        Vector4 offsetAndSize = spriteRenderer.sprite.OffsetAndSizeInTexture();
        Debug.Log(offsetAndSize);
        spriteRenderer.sharedMaterial.SetVector("_Offset_Size", offsetAndSize);
    }
    
    
}
