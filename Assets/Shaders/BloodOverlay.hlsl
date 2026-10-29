#ifndef BLOOD_OVERLAY_INCLUDED
#define BLOOD_OVERLAY_INCLUDED

// MaskRect: xy = mask world min, zw = 1 / mask world size (set globally as _BloodMaskRect)
// Tiling: blood texture repeats per world unit. For 1:1 pixels use PPU / texture size, e.g. 48 / 96 = 0.5

void BloodOverlay_float(float3 WorldPos, UnityTexture2D BloodMask, float4 MaskRect, UnityTexture2D BloodTex, float Tiling, float4 Tint, out float3 Color, out float Alpha) {
    float2 maskUV = (WorldPos.xy - MaskRect.xy) * MaskRect.zw;
    float mask = SAMPLE_TEXTURE2D(BloodMask.tex, BloodMask.samplerstate, maskUV).r;
    float4 blood = SAMPLE_TEXTURE2D(BloodTex.tex, BloodTex.samplerstate, WorldPos.xy * Tiling) * Tint;
    Color = blood.rgb;
    Alpha = mask * blood.a;
}

void BloodOverlay_half(half3 WorldPos, UnityTexture2D BloodMask, half4 MaskRect, UnityTexture2D BloodTex, half Tiling, half4 Tint, out half3 Color, out half Alpha) {
    float3 color;
    float alpha;
    BloodOverlay_float(WorldPos, BloodMask, MaskRect, BloodTex, Tiling, Tint, color, alpha);
    Color = color;
    Alpha = alpha;
}

#endif
