using UnityEngine;
using UnityEngine.UI;

public class Minimap : MonoBehaviour {

    public float zoom;
    public RawImage mapImage;
    public RectTransform playerDot;

    private Vector2 worldOrigin;
    private Vector2 worldSize;

    public void Init(Texture texture, Vector2 worldOrigin, Vector2 worldSize) {
        this.worldOrigin = worldOrigin;
        this.worldSize = worldSize;
        mapImage.texture = texture;
    }

    public void UpdateMinimap(Vector2 playerWorldPos) {
        if (worldSize.x <= 0f || worldSize.y <= 0f) return;

        float u = (playerWorldPos.x - worldOrigin.x) / worldSize.x;
        float v = (playerWorldPos.y - worldOrigin.y) / worldSize.y;

        float aspect = worldSize.x / worldSize.y;
        float zoomWithAspect = zoom * aspect;

        float offsetX = u + (1f - zoom) * 0.5f;
        float offsetY = v + (1f - zoomWithAspect) * 0.5f;

        Vector2 centeringVector = new Vector2(1f - zoom, 1f - zoomWithAspect) * 0.5f;

        const float maxNormalizedOffset = 0.25f;
        float zoomX = maxNormalizedOffset * zoom;
        float zoomY = maxNormalizedOffset * zoom * Mathf.Abs(aspect - 1f);
        
        float minXOffset = -zoomX + centeringVector.x;
        float maxXOffset = zoomX + centeringVector.x;

        float minYOffset = -zoomY + centeringVector.y;
        float maxYOffset = zoomY + centeringVector.y;

        float clampedXOffset = Mathf.Clamp(offsetX, minXOffset, maxXOffset);
        float clampedYOffset = Mathf.Clamp(offsetY, minYOffset, maxYOffset);

        Vector2 playerDotOffset = new(offsetX - clampedXOffset, offsetY - clampedYOffset);
        Vector2 pixelsPerNormalizedCoord = new(mapImage.rectTransform.rect.width / zoom, mapImage.rectTransform.rect.height / zoomWithAspect);
        playerDot.localPosition = playerDotOffset * pixelsPerNormalizedCoord;

        mapImage.uvRect = new Rect(clampedXOffset, clampedYOffset, zoom, zoomWithAspect);
    }

}
