using UnityEngine;
using VInspector;

public class CrystalFragmentCreator : MonoBehaviour {
    
    public float currentPixelsPerUnit;
    public Sprite[] allSpriteFragments;
    
    [Button]
    private void Create() {
        Sprite fullCystalSprite = GetComponent<SpriteRenderer>().sprite;
        
        foreach (Sprite sprite in allSpriteFragments) {
            GameObject fragment = new();
            fragment.transform.SetParent(transform);
            fragment.AddComponent<SpriteRenderer>().sprite = sprite;
            
            Vector4 mainOffsetSize = fullCystalSprite.OffsetAndSizeInTexture();
            Vector4 fragOffsetSize = sprite.OffsetAndSizeInTexture();
            
            Vector2 mainCenter = new(mainOffsetSize.x + (mainOffsetSize.z * 0.5f), mainOffsetSize.y + (mainOffsetSize.w * 0.5f));
            Vector2 fragCenter = new(fragOffsetSize.x + (fragOffsetSize.z * 0.5f),  fragOffsetSize.y + (fragOffsetSize.w * 0.5f));
            
            Vector4 localOffset = fragCenter - mainCenter;
            fragment.transform.localPosition = new Vector2(localOffset.x * sprite.texture.width, localOffset.y * sprite.texture.height) * (1f / currentPixelsPerUnit);
        }
    }
    
}
