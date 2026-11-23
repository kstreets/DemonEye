using UnityEngine;
using VInspector;

public class CrystalFragmentCreator : MonoBehaviour {

    public float currentPixelsPerUnit;
    public Sprite[] allSpriteFragments;

    // Lines the fragment's pivot up with where it sits in the texture, relative to the full crystal's pivot
    Vector2 FragmentLocalPosition(Sprite fullCrystal, Sprite fragment) {
        Vector2 mainPivotInTexture = fullCrystal.rect.position + fullCrystal.pivot;
        Vector2 fragPivotInTexture = fragment.rect.position + fragment.pivot;
        return (fragPivotInTexture - mainPivotInTexture) / currentPixelsPerUnit;
    }

    // Moves the existing fragments, for when a pivot changed after they were created
    [Button]
    void Reposition() {
        Sprite fullCystalSprite = GetComponent<SpriteRenderer>().sprite;
        foreach (Transform child in transform) {
            if (!child.TryGetComponent(out SpriteRenderer fragmentRenderer)) continue;
            if (System.Array.IndexOf(allSpriteFragments, fragmentRenderer.sprite) < 0) continue;
            child.localPosition = FragmentLocalPosition(fullCystalSprite, fragmentRenderer.sprite);
        }
    }

    [Button]
    void Create() {
        Sprite fullCystalSprite = GetComponent<SpriteRenderer>().sprite;

        foreach (Sprite sprite in allSpriteFragments) {
            GameObject fragment = new();
            fragment.transform.SetParent(transform);
            fragment.AddComponent<SpriteRenderer>().sprite = sprite;

            fragment.transform.localPosition = FragmentLocalPosition(fullCystalSprite, sprite);
        }
    }

}
