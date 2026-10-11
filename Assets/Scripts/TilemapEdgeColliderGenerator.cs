using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Generates EdgeCollider2Ds that trace the opaque pixels of this tilemap's sprites, so collision follows
// edge tiles whose visuals are inset from their cell bounds. Sprite renderers under additionalObjects are traced too.
// Output goes on a child object on the Default layer.
[RequireComponent(typeof(Tilemap))]
public class TilemapEdgeColliderGenerator : MonoBehaviour {

    [Tooltip("Sprite pixels with alpha at or above this count as solid")]
    [Range(0.01f, 1f)] public float alphaThreshold = 0.5f;
    [Tooltip("Max distance (in sprite pixels) the simplified outline may deviate from the traced pixel staircase")]
    public float simplifyTolerancePixels = 0.75f;
    [Tooltip("Outlines enclosing fewer pixels than this are discarded (specks, tiny holes in tile art)")]
    public int minLoopAreaPixels = 16;
    [Tooltip("Pushes the outline upward (in sprite pixels) on tiles with no tile above them, since those visual edges are too tight to walk against")]
    public int topEdgeOutsetPixels = 8;
    [Tooltip("Pushes the outline sideways (in sprite pixels) on tiles with no tile to their left/right")]
    public int sideEdgeOutsetPixels = 8;
    [Tooltip("Shifts the outline upward (in sprite pixels) on tiles with no tile below them, so the player can walk further into the cliff face art")]
    public int bottomEdgeInsetPixels = 0;
    public float edgeRadius = 0.1f;
    [Tooltip("Every active SpriteRenderer nested under these is traced along with the tiles, so their outlines merge into the same colliders")]
    public List<GameObject> additionalObjects = new();

#if UNITY_EDITOR

    private const string generatedObjectName = "GeneratedEdgeColliders";

    [VInspector.Button("Generate")]
    public void Generate() {
        Tilemap tilemap = GetComponent<Tilemap>();
        GridLayout.CellLayout layout = tilemap.layoutGrid.cellLayout;
        if (layout != GridLayout.CellLayout.Rectangle) {
            Debug.LogError($"{nameof(TilemapEdgeColliderGenerator)} only supports rectangular grids, got {layout}", this);
            return;
        }

        tilemap.CompressBounds();
        BoundsInt cellBounds = tilemap.cellBounds;
        List<MaskSprite> maskSprites = GatherMaskSprites(tilemap, cellBounds);
        if (maskSprites.Count == 0) {
            Debug.LogWarning("Tilemap and additional objects have no sprites", this);
            return;
        }

        float pixelsPerUnit = GetPixelsPerUnit(maskSprites);

        // Mask pixels are aligned to the cell grid and cover every sprite; pad by one cell so outer outlines and outsets fit
        Vector3 cellSize = tilemap.layoutGrid.cellSize;
        int pixelsPerCellX = Mathf.RoundToInt(cellSize.x * pixelsPerUnit);
        int pixelsPerCellY = Mathf.RoundToInt(cellSize.y * pixelsPerUnit);
        Vector2 pixelSize = new(cellSize.x / pixelsPerCellX, cellSize.y / pixelsPerCellY);
        const int paddingCells = 1;
        GetMaskCellRange(tilemap, maskSprites, paddingCells, out Vector2Int maskCellMin, out Vector2Int maskCellSize);
        int maskWidth = maskCellSize.x * pixelsPerCellX;
        int maskHeight = maskCellSize.y * pixelsPerCellY;
        Vector2 maskOrigin = tilemap.CellToLocal((Vector3Int)maskCellMin);

        bool[] mask = BuildMask(maskSprites, maskOrigin, pixelSize, maskWidth, maskHeight);
        // Inset before outsetting so the side outsets don't leave a foot sticking down below the shifted bottom edge
        InsetOpenBottomEdges(tilemap, cellBounds, mask, maskWidth, pixelsPerCellX, pixelsPerCellY, maskCellMin);
        OutsetOpenEdges(tilemap, cellBounds, mask, maskWidth, maskHeight, pixelsPerCellX, pixelsPerCellY, maskCellMin);
        List<List<Vector2Int>> loops = TraceOutlines(mask, maskWidth, maskHeight);

        ClearGenerated();
        GameObject output = new(generatedObjectName) { layer = LayerMask.NameToLayer("Default") };
        Undo.RegisterCreatedObjectUndo(output, "Generate Tilemap Edge Colliders");
        output.transform.SetParent(transform, false);

        int colliderCount = 0;
        int pointCount = 0;
        List<Vector2> simplified = new();
        foreach (List<Vector2Int> loop in loops) {
            if (Mathf.Abs(SignedArea(loop)) < minLoopAreaPixels) continue;

            SimplifyClosedLoop(loop, simplifyTolerancePixels, simplified);
            if (simplified.Count < 3) continue;

            Vector2[] points = new Vector2[simplified.Count + 1];
            for (int i = 0; i < simplified.Count; i++) {
                points[i] = maskOrigin + Vector2.Scale(simplified[i], pixelSize);
            }
            points[^1] = points[0];

            EdgeCollider2D edgeCol = output.AddComponent<EdgeCollider2D>();
            edgeCol.points = points;
            edgeCol.edgeRadius = edgeRadius;
            colliderCount++;
            pointCount += points.Length;
        }

        Debug.Log($"Generated {colliderCount} edge colliders ({pointCount} points) for {name}", this);
    }

    [VInspector.Button("Clear")]
    public void ClearGenerated() {
        Transform existing = transform.Find(generatedObjectName);
        if (existing != null) {
            Undo.DestroyObjectImmediate(existing.gameObject);
        }
    }

    // A sprite to trace into the mask, from a tile or a sprite renderer
    private struct MaskSprite {
        public Sprite sprite;
        public Matrix4x4 spriteToLocal; // Sprite space (units, pivot at origin) to tilemap local space
        public float alpha;
        public Object context;
    }

    private List<MaskSprite> GatherMaskSprites(Tilemap tilemap, BoundsInt cellBounds) {
        List<MaskSprite> maskSprites = new();

        foreach (Vector3Int cell in cellBounds.allPositionsWithin) {
            Sprite sprite = tilemap.GetSprite(cell);
            if (sprite == null) continue;
            maskSprites.Add(new() {
                sprite = sprite,
                spriteToLocal = Matrix4x4.Translate(tilemap.GetCellCenterLocal(cell)) * tilemap.orientationMatrix * tilemap.GetTransformMatrix(cell),
                alpha = tilemap.GetColor(cell).a * tilemap.color.a,
                context = this,
            });
        }

        HashSet<SpriteRenderer> addedRenderers = new(); // Objects in the list can be nested under each other
        Matrix4x4 worldToTilemapLocal = tilemap.transform.worldToLocalMatrix;
        foreach (GameObject additionalObject in additionalObjects) {
            if (additionalObject == null) continue;
            foreach (SpriteRenderer spriteRenderer in additionalObject.GetComponentsInChildren<SpriteRenderer>()) {
                if (!spriteRenderer.enabled || spriteRenderer.sprite == null || !addedRenderers.Add(spriteRenderer)) continue;
                if (spriteRenderer.drawMode != SpriteDrawMode.Simple) {
                    Debug.LogWarning($"{spriteRenderer.name} uses draw mode {spriteRenderer.drawMode}, it will be traced as if it were Simple", spriteRenderer);
                }

                Vector3 flip = new(spriteRenderer.flipX ? -1f : 1f, spriteRenderer.flipY ? -1f : 1f, 1f);
                maskSprites.Add(new() {
                    sprite = spriteRenderer.sprite,
                    spriteToLocal = worldToTilemapLocal * spriteRenderer.transform.localToWorldMatrix * Matrix4x4.Scale(flip),
                    alpha = spriteRenderer.color.a,
                    context = spriteRenderer,
                });
            }
        }

        return maskSprites;
    }

    private static float GetPixelsPerUnit(List<MaskSprite> maskSprites) {
        float pixelsPerUnit = maskSprites[0].sprite.pixelsPerUnit;
        foreach (MaskSprite maskSprite in maskSprites) {
            if (Mathf.Approximately(pixelsPerUnit, maskSprite.sprite.pixelsPerUnit)) continue;
            Debug.LogWarning($"Sprite {maskSprite.sprite.name} has PPU {maskSprite.sprite.pixelsPerUnit}, expected {pixelsPerUnit}. Outline will be sampled at {pixelsPerUnit} PPU", maskSprite.context);
            break;
        }
        return pixelsPerUnit;
    }

    private static void GetSpriteLocalBounds(MaskSprite maskSprite, out Vector2 localMin, out Vector2 localMax) {
        Sprite sprite = maskSprite.sprite;
        Vector2 spriteMin = -sprite.pivot / sprite.pixelsPerUnit;
        Vector2 spriteMax = (sprite.rect.size - sprite.pivot) / sprite.pixelsPerUnit;
        localMin = new(float.MaxValue, float.MaxValue);
        localMax = new(float.MinValue, float.MinValue);
        for (int corner = 0; corner < 4; corner++) {
            Vector2 spriteCorner = new((corner & 1) == 0 ? spriteMin.x : spriteMax.x, (corner & 2) == 0 ? spriteMin.y : spriteMax.y);
            Vector2 localCorner = maskSprite.spriteToLocal.MultiplyPoint3x4(spriteCorner);
            localMin = Vector2.Min(localMin, localCorner);
            localMax = Vector2.Max(localMax, localCorner);
        }
    }

    // The range of cells the mask covers, so every sprite fits with padding around it
    private static void GetMaskCellRange(Tilemap tilemap, List<MaskSprite> maskSprites, int paddingCells, out Vector2Int maskCellMin, out Vector2Int maskCellSize) {
        Vector2Int min = new(int.MaxValue, int.MaxValue);
        Vector2Int max = new(int.MinValue, int.MinValue);
        foreach (MaskSprite maskSprite in maskSprites) {
            GetSpriteLocalBounds(maskSprite, out Vector2 localMin, out Vector2 localMax);
            min = Vector2Int.Min(min, (Vector2Int)tilemap.LocalToCell(localMin));
            max = Vector2Int.Max(max, (Vector2Int)tilemap.LocalToCell(localMax));
        }
        maskCellMin = min - Vector2Int.one * paddingCells;
        maskCellSize = max - min + Vector2Int.one * (1 + paddingCells * 2);
    }

    // Rasterizes every sprite's alpha into a grid-aligned solid mask by inverse-mapping each mask pixel
    // into sprite space, so flips/rotations/scales are respected
    private bool[] BuildMask(List<MaskSprite> maskSprites, Vector2 maskOrigin, Vector2 pixelSize, int maskWidth, int maskHeight) {
        bool[] mask = new bool[maskWidth * maskHeight];
        Dictionary<Texture2D, Color32[]> texturePixelCache = new();
        byte alphaThresholdByte = (byte)Mathf.Clamp(Mathf.CeilToInt(alphaThreshold * 255f), 1, 255);

        foreach (MaskSprite maskSprite in maskSprites) {
            if (maskSprite.alpha < alphaThreshold) continue;
            byte spriteThreshold = (byte)Mathf.Clamp(Mathf.CeilToInt(alphaThresholdByte / maskSprite.alpha), 1, 255);

            Sprite sprite = maskSprite.sprite;
            Texture2D texture = sprite.texture;
            if (!texturePixelCache.TryGetValue(texture, out Color32[] texturePixels)) {
                texturePixels = ReadTexturePixels(texture);
                texturePixelCache[texture] = texturePixels;
            }

            Matrix4x4 localToSprite = maskSprite.spriteToLocal.inverse;
            Rect spriteRect = sprite.rect;
            Vector2 pivot = sprite.pivot;
            float ppu = sprite.pixelsPerUnit;

            // Bounding box of the transformed sprite in mask pixel coordinates
            GetSpriteLocalBounds(maskSprite, out Vector2 localMin, out Vector2 localMax);
            int minX = Mathf.Max(0, Mathf.FloorToInt((localMin.x - maskOrigin.x) / pixelSize.x));
            int minY = Mathf.Max(0, Mathf.FloorToInt((localMin.y - maskOrigin.y) / pixelSize.y));
            int maxX = Mathf.Min(maskWidth - 1, Mathf.CeilToInt((localMax.x - maskOrigin.x) / pixelSize.x));
            int maxY = Mathf.Min(maskHeight - 1, Mathf.CeilToInt((localMax.y - maskOrigin.y) / pixelSize.y));

            for (int my = minY; my <= maxY; my++) {
                for (int mx = minX; mx <= maxX; mx++) {
                    int maskIndex = my * maskWidth + mx;
                    if (mask[maskIndex]) continue;

                    Vector2 localPos = maskOrigin + Vector2.Scale(new Vector2(mx + 0.5f, my + 0.5f), pixelSize);
                    Vector2 spritePos = (Vector2)localToSprite.MultiplyPoint3x4(localPos) * ppu + pivot;
                    if (spritePos.x < 0f || spritePos.y < 0f || spritePos.x >= spriteRect.width || spritePos.y >= spriteRect.height) continue;

                    int texX = (int)spriteRect.x + (int)spritePos.x;
                    int texY = (int)spriteRect.y + (int)spritePos.y;
                    if (texturePixels[texY * texture.width + texX].a >= spriteThreshold) {
                        mask[maskIndex] = true;
                    }
                }
            }
        }

        return mask;
    }

    // For tiles with nothing below them, removes the bottommost solid pixels of each pixel column so the outline sits higher up.
    // Cliff face art can hang below the tile's cell, so the column's bottom is searched for below the cell too.
    private void InsetOpenBottomEdges(Tilemap tilemap, BoundsInt cellBounds, bool[] mask, int maskWidth, int pixelsPerCellX, int pixelsPerCellY, Vector2Int maskCellMin) {
        int inset = Mathf.Max(0, bottomEdgeInsetPixels);
        if (inset == 0) return;

        // Gather every column's bottom pixel before modifying the mask so the result doesn't depend on processing order
        List<Vector2Int> columnBottoms = new();
        foreach (Vector3Int cell in cellBounds.allPositionsWithin) {
            if (tilemap.GetSprite(cell) == null || tilemap.GetSprite(cell + Vector3Int.down) != null) continue;

            int cellMaskX = (cell.x - maskCellMin.x) * pixelsPerCellX;
            int cellMaskY = (cell.y - maskCellMin.y) * pixelsPerCellY;
            int cellTopY = cellMaskY + pixelsPerCellY - 1;
            int lowestSearchY = Mathf.Max(0, cellMaskY - pixelsPerCellY); // The padding is one cell, so art can't hang further than that

            for (int x = cellMaskX; x < cellMaskX + pixelsPerCellX; x++) {
                int y = cellMaskY;
                if (mask[y * maskWidth + x]) {
                    // Follow the art down past the cell's bottom to where it ends
                    while (y > lowestSearchY && mask[(y - 1) * maskWidth + x]) y--;
                } else {
                    // Art doesn't reach the cell's bottom, find where it starts
                    while (y < cellTopY && !mask[y * maskWidth + x]) y++;
                    if (!mask[y * maskWidth + x]) continue;
                }
                columnBottoms.Add(new(x, y));
            }
        }

        // Stops at the first empty pixel so thin art doesn't get cut through
        foreach (Vector2Int bottom in columnBottoms) {
            for (int y = bottom.y; y < bottom.y + inset && mask[y * maskWidth + bottom.x]; y++) {
                mask[y * maskWidth + bottom.x] = false;
            }
        }
    }

    // For tiles with nothing above/left/right of them, extends the outermost solid pixel of each pixel lane toward the open side
    // so the outline sits further out. Bottom edges are handled by InsetOpenBottomEdges instead, since their cliff face art keeps the outline far enough out.
    private void OutsetOpenEdges(Tilemap tilemap, BoundsInt cellBounds, bool[] mask, int maskWidth, int maskHeight, int pixelsPerCellX, int pixelsPerCellY, Vector2Int maskCellMin) {
        (Vector3Int dir, int outset)[] openSides = {
            (Vector3Int.up, Mathf.Clamp(topEdgeOutsetPixels, 0, pixelsPerCellY)),
            (Vector3Int.left, Mathf.Clamp(sideEdgeOutsetPixels, 0, pixelsPerCellX)),
            (Vector3Int.right, Mathf.Clamp(sideEdgeOutsetPixels, 0, pixelsPerCellX)),
        };

        // Gather which sides are open before modifying the mask so the result doesn't depend on processing order
        List<(Vector3Int cell, int side)> openEdges = new();
        foreach (Vector3Int cell in cellBounds.allPositionsWithin) {
            if (tilemap.GetSprite(cell) == null) continue;
            for (int side = 0; side < openSides.Length; side++) {
                if (openSides[side].outset > 0 && tilemap.GetSprite(cell + openSides[side].dir) == null) {
                    openEdges.Add((cell, side));
                }
            }
        }

        foreach ((Vector3Int cell, int side) in openEdges) {
            (Vector3Int dir, int outset) = openSides[side];
            int cellMaskX = (cell.x - maskCellMin.x) * pixelsPerCellX;
            int cellMaskY = (cell.y - maskCellMin.y) * pixelsPerCellY;
            bool vertical = dir.y != 0;
            int laneCount = vertical ? pixelsPerCellX : pixelsPerCellY;
            int laneLength = vertical ? pixelsPerCellY : pixelsPerCellX;

            for (int lane = 0; lane < laneCount; lane++) {
                // Start at the cell's pixel on the open side and scan inward for the first solid pixel
                Vector2Int pixel = vertical
                    ? new Vector2Int(cellMaskX + lane, dir.y > 0 ? cellMaskY + pixelsPerCellY - 1 : cellMaskY)
                    : new Vector2Int(dir.x > 0 ? cellMaskX + pixelsPerCellX - 1 : cellMaskX, cellMaskY + lane);
                bool foundSolid = false;
                for (int step = 0; step < laneLength; step++) {
                    if (mask[pixel.y * maskWidth + pixel.x]) {
                        foundSolid = true;
                        break;
                    }
                    pixel -= (Vector2Int)dir;
                }
                if (!foundSolid) continue;

                for (int fill = 0; fill < outset; fill++) {
                    pixel += (Vector2Int)dir;
                    if (pixel.x < 0 || pixel.y < 0 || pixel.x >= maskWidth || pixel.y >= maskHeight) break;
                    mask[pixel.y * maskWidth + pixel.x] = true;
                }
            }
        }
    }

    // Copies texture pixels through a RenderTexture so textures without Read/Write enabled still work
    private static Color32[] ReadTexturePixels(Texture2D texture) {
        if (texture.isReadable) {
            return texture.GetPixels32();
        }

        RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        RenderTexture previousActive = RenderTexture.active;
        Graphics.Blit(texture, rt);
        RenderTexture.active = rt;
        Texture2D readable = new(texture.width, texture.height, TextureFormat.RGBA32, false, true);
        readable.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        readable.Apply();
        RenderTexture.active = previousActive;
        RenderTexture.ReleaseTemporary(rt);

        Color32[] pixels = readable.GetPixels32();
        DestroyImmediate(readable);
        return pixels;
    }

    private static readonly Vector2Int[] directionSteps = { new(1, 0), new(0, 1), new(-1, 0), new(0, -1) };

    // Walks the boundary between solid and empty mask pixels, returning closed loops of lattice corners.
    // Edges are directed with solid on the left; at saddle points we turn left so diagonal-only neighbors stay separate loops.
    private static List<List<Vector2Int>> TraceOutlines(bool[] mask, int width, int height) {
        int latticeWidth = width + 1;
        int vertexCount = latticeWidth * (height + 1);
        // Each lattice vertex has at most two outgoing boundary edges (two only at saddle points); bit per direction
        byte[] outgoing = new byte[vertexCount];

        bool Solid(int x, int y) => x >= 0 && y >= 0 && x < width && y < height && mask[y * width + x];

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                if (!mask[y * width + x]) continue;
                if (!Solid(x, y - 1)) outgoing[y * latticeWidth + x] |= 1 << 0;             // bottom, +x
                if (!Solid(x + 1, y)) outgoing[y * latticeWidth + x + 1] |= 1 << 1;         // right, +y
                if (!Solid(x, y + 1)) outgoing[(y + 1) * latticeWidth + x + 1] |= 1 << 2;   // top, -x
                if (!Solid(x - 1, y)) outgoing[(y + 1) * latticeWidth + x] |= 1 << 3;       // left, -y
            }
        }

        List<List<Vector2Int>> loops = new();
        for (int startVertex = 0; startVertex < vertexCount; startVertex++) {
            if (outgoing[startVertex] == 0) continue;

            List<Vector2Int> loop = new();
            Vector2Int pos = new(startVertex % latticeWidth, startVertex / latticeWidth);
            int prevDir = -1;
            int vertex = startVertex;
            while (outgoing[vertex] != 0) {
                int dir = PickDirection(outgoing[vertex], prevDir);
                outgoing[vertex] &= (byte)~(1 << dir);
                if (dir != prevDir) {
                    loop.Add(pos);
                }
                pos += directionSteps[dir];
                vertex = pos.y * latticeWidth + pos.x;
                prevDir = dir;
            }

            // The walk ends back at the start; drop the start corner if the loop passes straight through it
            if (loop.Count > 2 && pos == loop[0]) {
                Vector2Int firstDir = loop[1] - loop[0];
                Vector2Int lastDir = loop[0] - loop[^1];
                if (firstDir.x * lastDir.y - firstDir.y * lastDir.x == 0) {
                    loop.RemoveAt(0);
                }
            }
            loops.Add(loop);
        }

        return loops;
    }

    private static int PickDirection(byte options, int prevDir) {
        if (prevDir < 0) {
            for (int dir = 0; dir < 4; dir++) {
                if ((options & (1 << dir)) != 0) return dir;
            }
        }
        int left = (prevDir + 1) & 3;
        if ((options & (1 << left)) != 0) return left;
        if ((options & (1 << prevDir)) != 0) return prevDir;
        return (prevDir + 3) & 3;
    }

    private static float SignedArea(List<Vector2Int> loop) {
        long doubleArea = 0;
        for (int i = 0; i < loop.Count; i++) {
            Vector2Int a = loop[i];
            Vector2Int b = loop[(i + 1) % loop.Count];
            doubleArea += (long)a.x * b.y - (long)b.x * a.y;
        }
        return doubleArea * 0.5f;
    }

    // Douglas-Peucker on a closed loop: split at the vertex farthest from the first, then simplify each half
    private static void SimplifyClosedLoop(List<Vector2Int> loop, float tolerance, List<Vector2> result) {
        result.Clear();
        int count = loop.Count;
        if (count < 3) return;

        int farthest = 0;
        float farthestSqrDist = -1f;
        for (int i = 1; i < count; i++) {
            float sqrDist = (loop[i] - loop[0]).sqrMagnitude;
            if (sqrDist > farthestSqrDist) {
                farthestSqrDist = sqrDist;
                farthest = i;
            }
        }

        bool[] keep = new bool[count];
        keep[0] = true;
        keep[farthest] = true;

        Stack<(int start, int end)> ranges = new();
        ranges.Push((0, farthest));
        ranges.Push((farthest, count)); // index == count wraps back to 0
        while (ranges.Count > 0) {
            (int start, int end) = ranges.Pop();
            if (end - start < 2) continue;

            Vector2 a = loop[start % count];
            Vector2 b = loop[end % count];
            int maxIndex = -1;
            float maxDist = tolerance;
            for (int i = start + 1; i < end; i++) {
                float dist = DistanceToSegment(loop[i], a, b);
                if (dist > maxDist) {
                    maxDist = dist;
                    maxIndex = i;
                }
            }

            if (maxIndex != -1) {
                keep[maxIndex] = true;
                ranges.Push((start, maxIndex));
                ranges.Push((maxIndex, end));
            }
        }

        for (int i = 0; i < count; i++) {
            if (keep[i]) result.Add(loop[i]);
        }
    }

    private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b) {
        Vector2 ab = b - a;
        float sqrLength = ab.sqrMagnitude;
        if (sqrLength == 0f) return Vector2.Distance(point, a);
        float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / sqrLength);
        return Vector2.Distance(point, a + ab * t);
    }

#endif

}
