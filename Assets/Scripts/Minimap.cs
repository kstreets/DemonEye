using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Minimap : MonoBehaviour {

    public float zoom;
    public RawImage mapImage;
    public RectTransform playerDot;
    public RectTransform portalDotTemplate;
    public RectTransform portalArrowTemplate;
    public float portalArrowBorderInset = 14f;
    public float portalDotEdgeMargin = 8f; // Switch to the arrow this far before the dot reaches the edge
    Vector2 worldOrigin;
    Vector2 worldSize;
    Vector2 clampedOffset;
    Vector2 pixelsPerNormalizedCoord;
    readonly List<RectTransform> portalDots = new();
    readonly List<RectTransform> portalArrows = new();

    float ZoomWithAspect => zoom * (worldSize.x / worldSize.y);

    public void Init(Texture texture, Vector2 worldOrigin, Vector2 worldSize) {
        this.worldOrigin = worldOrigin;
        this.worldSize = worldSize;
        mapImage.texture = texture;
        HideMarkers(portalDots, portalDotTemplate, 0);
        HideMarkers(portalArrows, portalArrowTemplate, 0);
    }

    public void UpdateMinimap(Vector2 playerWorldPos) {
        if (worldSize.x <= 0f || worldSize.y <= 0f) return;

        float aspect = worldSize.x / worldSize.y;
        Vector2 offset = NormalizedOffset(playerWorldPos);
        Vector2 centeringVector = new Vector2(1f - zoom, 1f - ZoomWithAspect) * 0.5f;

        const float maxNormalizedOffset = 0.25f;
        float zoomX = maxNormalizedOffset * zoom;
        float zoomY = maxNormalizedOffset * zoom * Mathf.Abs(aspect - 1f);

        clampedOffset = new(
            Mathf.Clamp(offset.x, -zoomX + centeringVector.x, zoomX + centeringVector.x),
            Mathf.Clamp(offset.y, -zoomY + centeringVector.y, zoomY + centeringVector.y));
        pixelsPerNormalizedCoord = new(mapImage.rectTransform.rect.width / zoom, mapImage.rectTransform.rect.height / ZoomWithAspect);

        playerDot.localPosition = WorldToLocal(playerWorldPos);
        mapImage.uvRect = new Rect(clampedOffset.x, clampedOffset.y, zoom, ZoomWithAspect);
    }

    // Portals inside the map get a dot, ones outside get an arrow on the border pointing at them
    public void UpdatePortalMarkers(List<Vector2> portalWorldPositions) {
        Rect rect = ((RectTransform)transform).rect;
        Vector2 dotBounds = rect.size * 0.5f - portalDotTemplate.rect.size * 0.5f - Vector2.one * portalDotEdgeMargin;
        Vector2 arrowBounds = rect.size * 0.5f - Vector2.one * portalArrowBorderInset;
        Vector2 playerPos = Vector2.Max(-arrowBounds, Vector2.Min(arrowBounds, playerDot.localPosition));

        int dotCount = 0;
        int arrowCount = 0;
        foreach (Vector2 worldPos in portalWorldPositions) {
            Vector2 localPos = WorldToLocal(worldPos);
            bool onMap = Mathf.Abs(localPos.x) <= dotBounds.x && Mathf.Abs(localPos.y) <= dotBounds.y;
            if (onMap) {
                GetMarker(portalDots, portalDotTemplate, dotCount++).localPosition = localPos;
                continue;
            }

            // Cast from the player dot to the inset border so the arrow hugs the edge
            Vector2 dir = (localPos - playerPos).normalized;
            float dist = float.MaxValue;
            if (dir.x != 0f) dist = Mathf.Min(dist, (Mathf.Sign(dir.x) * arrowBounds.x - playerPos.x) / dir.x);
            if (dir.y != 0f) dist = Mathf.Min(dist, (Mathf.Sign(dir.y) * arrowBounds.y - playerPos.y) / dir.y);

            RectTransform arrow = GetMarker(portalArrows, portalArrowTemplate, arrowCount++);
            arrow.localPosition = playerPos + dir * dist;
            arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f); // The sprite points up
        }

        HideMarkers(portalDots, portalDotTemplate, dotCount);
        HideMarkers(portalArrows, portalArrowTemplate, arrowCount);
    }

    Vector2 NormalizedOffset(Vector2 worldPos) {
        float u = (worldPos.x - worldOrigin.x) / worldSize.x;
        float v = (worldPos.y - worldOrigin.y) / worldSize.y;
        return new(u + (1f - zoom) * 0.5f, v + (1f - ZoomWithAspect) * 0.5f);
    }

    Vector2 WorldToLocal(Vector2 worldPos) => (NormalizedOffset(worldPos) - clampedOffset) * pixelsPerNormalizedCoord;

    static RectTransform GetMarker(List<RectTransform> markers, RectTransform template, int index) {
        if (markers.Count == 0) markers.Add(template);
        while (markers.Count <= index) {
            markers.Add(Instantiate(template, template.parent));
        }
        RectTransform marker = markers[index];
        marker.gameObject.SetActive(true);
        return marker;
    }

    static void HideMarkers(List<RectTransform> markers, RectTransform template, int fromIndex) {
        if (markers.Count == 0) {
            template.gameObject.SetActive(false);
            return;
        }
        for (int i = fromIndex; i < markers.Count; i++) {
            markers[i].gameObject.SetActive(false);
        }
    }

}
