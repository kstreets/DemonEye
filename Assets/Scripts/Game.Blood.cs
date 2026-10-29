using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public partial class Game {

    public struct BloodStamp {
        public Vector2 position;
        public Sprite sprite;
        public bool flipX;
    }

    private void InitBlood() {
        blood.cmd = new() { name = "Blood Mask" };
        blood.materialProperyBlock = new();
        blood.stampQuad = new() {
            name = "Blood Stamp Quad",
            vertices = new Vector3[] { new(-0.5f, -0.5f), new(0.5f, -0.5f), new(-0.5f, 0.5f), new(0.5f, 0.5f) },
            uv = new Vector2[] { new(0f, 0f), new(1f, 0f), new(0f, 1f), new(1f, 1f) },
            triangles = new[] { 0, 2, 1, 2, 3, 1 },
        };

        // The overlay shader works purely in world space, so the sprite only needs to define a 1x1 unit quad
        Texture2D whiteTex = Texture2D.whiteTexture;
        blood.overlay.sprite = Sprite.Create(whiteTex, new(0, 0, whiteTex.width, whiteTex.height), new(0.5f, 0.5f), whiteTex.width);
        blood.overlay.gameObject.SetActive(false);
    }

    private void InitRaidBlood() {
        Bounds mapBounds = curRaid.mapInstance.mainTilemapRenderer.bounds;
        Vector2 min = new(Mathf.Floor(mapBounds.min.x), Mathf.Floor(mapBounds.min.y));
        Vector2 max = new(Mathf.Ceil(mapBounds.max.x), Mathf.Ceil(mapBounds.max.y));
        Vector2 size = max - min;

        int width = Mathf.Min(Mathf.CeilToInt(size.x * blood.texelsPerUnit), SystemInfo.maxTextureSize);
        int height = Mathf.Min(Mathf.CeilToInt(size.y * blood.texelsPerUnit), SystemInfo.maxTextureSize);

        bool needsNewMask = blood.mask == null || blood.mask.width != width || blood.mask.height != height;
        if (needsNewMask) {
            if (blood.mask != null) {
                blood.mask.Release();
                Destroy(blood.mask);
            }
            blood.mask = new(width, height, GraphicsFormat.R8_UNorm, GraphicsFormat.None) {
                name = "Blood Mask",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            blood.mask.Create();
        }

        blood.maskWorldRect = new(min, size);
        Shader.SetGlobalTexture(ShaderIds.bloodMask, blood.mask);
        Shader.SetGlobalVector(ShaderIds.bloodMaskRect, new(min.x, min.y, 1f / size.x, 1f / size.y));

        Transform overlayTrans = blood.overlay.transform;
        overlayTrans.position = new(min.x + size.x * 0.5f, min.y + size.y * 0.5f, overlayTrans.position.z);
        overlayTrans.localScale = new(size.x, size.y, 1f);
        blood.overlay.gameObject.SetActive(true);

        blood.pendingStamps.Clear();
        CommandBuffer cmd = blood.cmd;
        cmd.Clear();
        cmd.SetRenderTarget(blood.mask);
        cmd.ClearRenderTarget(false, true, Color.clear);
        Graphics.ExecuteCommandBuffer(cmd);
    }

    private void DeinitRaidBlood() {
        blood.pendingStamps.Clear();
        blood.overlay.gameObject.SetActive(false);
    }

    private void StampBlood(Vector2 position) {
        Sprite[] masks = blood.splatterMasks;
        if (masks.Length == 0) return;

        blood.pendingStamps.Add(new() {
            position = position,
            sprite = masks[Random.Range(0, masks.Length)],
            flipX = Random.value < 0.5f,
        });
    }

    private void FlushBloodStamps() {
        if (blood.pendingStamps.Count == 0) return;

        CommandBuffer cmd = blood.cmd;
        cmd.Clear();
        cmd.SetRenderTarget(blood.mask);

        float texelsPerUnit = blood.texelsPerUnit;
        foreach (BloodStamp stamp in blood.pendingStamps) {
            Sprite sprite = stamp.sprite;
            Texture2D tex = sprite.texture;
            Rect texRect = sprite.textureRect;

            blood.materialProperyBlock.SetTexture(ShaderIds.mainTex, tex);
            blood.materialProperyBlock.SetVector(ShaderIds.mainTexST, new(texRect.width / tex.width, texRect.height / tex.height, texRect.x / tex.width, texRect.y / tex.height));

            // One sprite pixel per mask texel regardless of the sprite's import PPU, snapped to the texel grid so the pixels stay crisp
            Vector2 sizeInUnits = texRect.size / texelsPerUnit;
            Vector2 snappedPos = new(Mathf.Round(stamp.position.x * texelsPerUnit) / texelsPerUnit, Mathf.Round(stamp.position.y * texelsPerUnit) / texelsPerUnit);
            
            // Odd sized sprites need a half texel shift so their edges land on texel boundaries instead of texel centers
            snappedPos.x += (int)texRect.width % 2 == 1 ? 0.5f / texelsPerUnit : 0f;
            snappedPos.y += (int)texRect.height % 2 == 1 ? 0.5f / texelsPerUnit : 0f;
            Vector3 scale = new(stamp.flipX ? -sizeInUnits.x : sizeInUnits.x, sizeInUnits.y, 1f);

            // Command buffers copy the property block when recording, so reusing it per stamp is safe
            cmd.DrawMesh(blood.stampQuad, Matrix4x4.TRS(snappedPos, Quaternion.identity, scale), blood.stampMaterial, 0, 0, blood.materialProperyBlock);
        }

        Graphics.ExecuteCommandBuffer(cmd);
        blood.pendingStamps.Clear();
    }

}
