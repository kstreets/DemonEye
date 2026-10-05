using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Generates EdgeCollider2Ds that trace the opaque pixels of this tilemap's sprites, so collision follows
// edge tiles whose visuals are inset from their cell bounds. Output goes on a child object on the Default layer.
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
    public float edgeRadius = 0.1f;

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
        if (cellBounds.size.x <= 0 || cellBounds.size.y <= 0) {
            Debug.LogWarning("Tilemap has no tiles", this);
            return;
        }

        if (!TryGetPixelsPerUnit(tilemap, cellBounds, out float pixelsPerUnit)) {
            Debug.LogWarning("Tilemap has no sprites", this);
            return;
        }

        // Mask pixels are aligned to the cell grid; pad by one cell so oversized sprites and outer outlines fit
        Vector3 cellSize = tilemap.layoutGrid.cellSize;
        int pixelsPerCellX = Mathf.RoundToInt(cellSize.x * pixelsPerUnit);
        int pixelsPerCellY = Mathf.RoundToInt(cellSize.y * pixelsPerUnit);
        Vector2 pixelSize = new(cellSize.x / pixelsPerCellX, cellSize.y / pixelsPerCellY);
        const int paddingCells = 1;
        int maskWidth = (cellBounds.size.x + paddingCells * 2) * pixelsPerCellX;
        int maskHeight = (cellBounds.size.y + paddingCells * 2) * pixelsPerCellY;
        Vector2 maskOrigin = (Vector2)tilemap.CellToLocal(cellBounds.min) - new Vector2(cellSize.x, cellSize.y) * paddingCells;

        bool[] mask = BuildMask(tilemap, cellBounds, maskOrigin, pixelSize, maskWidth, maskHeight);
        OutsetOpenEdges(tilemap, cellBounds, mask, maskWidth, maskHeight, pixelsPerCellX, pixelsPerCellY, paddingCells);
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

    private bool TryGetPixelsPerUnit(Tilemap tilemap, BoundsInt cellBounds, out float pixelsPerUnit) {
        pixelsPerUnit = 0f;
        foreach (Vector3Int cell in cellBounds.allPositionsWithin) {
            Sprite sprite = tilemap.GetSprite(cell);
            if (sprite == null) continue;

            if (pixelsPerUnit == 0f) {
                pixelsPerUnit = sprite.pixelsPerUnit;
            } else if (!Mathf.Approximately(pixelsPerUnit, sprite.pixelsPerUnit)) {
                Debug.LogWarning($"Sprite {sprite.name} has PPU {sprite.pixelsPerUnit}, expected {pixelsPerUnit}. Outline will be sampled at {pixelsPerUnit} PPU", sprite);
                return true;
            }
        }
        return pixelsPerUnit > 0f;
    }

    // Rasterizes every tile's sprite alpha into a grid-aligned solid mask by inverse-mapping each mask pixel
    // into sprite space, so tile flips/rotations are respected
    private bool[] BuildMask(Tilemap tilemap, BoundsInt cellBounds, Vector2 maskOrigin, Vector2 pixelSize, int maskWidth, int maskHeight) {
        bool[] mask = new bool[maskWidth * maskHeight];
        Dictionary<Texture2D, Color32[]> texturePixelCache = new();
        byte alphaThresholdByte = (byte)Mathf.Clamp(Mathf.CeilToInt(alphaThreshold * 255f), 1, 255);

        foreach (Vector3Int cell in cellBounds.allPositionsWithin) {
            Sprite sprite = tilemap.GetSprite(cell);
            if (sprite == null) continue;

            float tileAlpha = tilemap.GetColor(cell).a * tilemap.color.a;
            if (tileAlpha < alphaThreshold) continue;
            byte cellThreshold = (byte)Mathf.Clamp(Mathf.CeilToInt(alphaThresholdByte / tileAlpha), 1, 255);

            Texture2D texture = sprite.texture;
            if (!texturePixelCache.TryGetValue(texture, out Color32[] texturePixels)) {
                texturePixels = ReadTexturePixels(texture);
                texturePixelCache[texture] = texturePixels;
            }

            Matrix4x4 spriteToLocal = Matrix4x4.Translate(tilemap.GetCellCenterLocal(cell)) * tilemap.orientationMatrix * tilemap.GetTransformMatrix(cell);
            Matrix4x4 localToSprite = spriteToLocal.inverse;
            Rect spriteRect = sprite.rect;
            Vector2 pivot = sprite.pivot;
            float ppu = sprite.pixelsPerUnit;

            // Bounding box of the transformed sprite in mask pixel coordinates
            Vector2 spriteMin = -pivot / ppu;
            Vector2 spriteMax = (spriteRect.size - pivot) / ppu;
            Vector2 localMin = new(float.MaxValue, float.MaxValue);
            Vector2 localMax = new(float.MinValue, float.MinValue);
            for (int corner = 0; corner < 4; corner++) {
                Vector2 spriteCorner = new((corner & 1) == 0 ? spriteMin.x : spriteMax.x, (corner & 2) == 0 ? spriteMin.y : spriteMax.y);
                Vector2 localCorner = spriteToLocal.MultiplyPoint3x4(spriteCorner);
                localMin = Vector2.Min(localMin, localCorner);
                localMax = Vector2.Max(localMax, localCorner);
            }
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
                    if (texturePixels[texY * texture.width + texX].a >= cellThreshold) {
                        mask[maskIndex] = true;
                    }
                }
            }
        }

        return mask;
    }

    // For tiles with nothing above/left/right of them, extends the outermost solid pixel of each pixel lane toward the open side
    // so the outline sits further out. Bottom edges are left alone since their cliff face art already keeps the outline far enough out.
    private void OutsetOpenEdges(Tilemap tilemap, BoundsInt cellBounds, bool[] mask, int maskWidth, int maskHeight, int pixelsPerCellX, int pixelsPerCellY, int paddingCells) {
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
            int cellMaskX = (cell.x - cellBounds.xMin + paddingCells) * pixelsPerCellX;
            int cellMaskY = (cell.y - cellBounds.yMin + paddingCells) * pixelsPerCellY;
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
