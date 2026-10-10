using UnityEngine;

public static class ShaderIds {

    // Material properties
    public static readonly int active = Shader.PropertyToID("_Active");
    public static readonly int aspectRatio = Shader.PropertyToID("_AspectRatio");
    public static readonly int completion = Shader.PropertyToID("_Completion");
    public static readonly int damageFlashTint = Shader.PropertyToID("_DamageFlashTint");
    public static readonly int dissolve = Shader.PropertyToID("_Dissolve");
    public static readonly int dissolveColor = Shader.PropertyToID("_DissolveColor");
    public static readonly int fill = Shader.PropertyToID("_Fill");
    public static readonly int fillMask = Shader.PropertyToID("_FillMask");
    public static readonly int hsvChannelMask = Shader.PropertyToID("_HSVChannelMask");
    public static readonly int hsvColor = Shader.PropertyToID("_HSVColor");
    public static readonly int intoOffsetSize = Shader.PropertyToID("_IntoOffset_Size");
    public static readonly int offsetSize = Shader.PropertyToID("_Offset_Size");
    public static readonly int opacity = Shader.PropertyToID("_Opacity");
    public static readonly int rotation = Shader.PropertyToID("_Rotation");
    public static readonly int useSmoothing = Shader.PropertyToID("_UseSmoothing");

    // Globals
    public static readonly int unscaledTime = Shader.PropertyToID("_UnscaledTime");
    public static readonly int waterMap = Shader.PropertyToID("_WaterMap");
    public static readonly int waterOcclusionMap = Shader.PropertyToID("_WaterOcclusionMap");
    public static readonly int waterUVScaler = Shader.PropertyToID("_WaterUVScaler");

    // Compute shader params
    public static readonly int width = Shader.PropertyToID("_width");
    public static readonly int height = Shader.PropertyToID("_height");
    public static readonly int input = Shader.PropertyToID("_Input");
    public static readonly int result = Shader.PropertyToID("_Result");
    public static readonly int verticalSampleOffset = Shader.PropertyToID("_verticalSampleOffset");
    public static readonly int camWorldXInPixels = Shader.PropertyToID("_camWorldXInPixels");
    public static readonly int pixelsPerUnit = Shader.PropertyToID("_pixelsPerUnit");
    public static readonly int waveStride = Shader.PropertyToID("_waveStride");
    public static readonly int startReflectionFade = Shader.PropertyToID("_startReflectionFade");
    public static readonly int endReflectionFade = Shader.PropertyToID("_endReflectionFade");
    public static readonly int waterFillColor = Shader.PropertyToID("_waterFillColor");
    public static readonly int sinOffsetInRadians = Shader.PropertyToID("_sinOffsetInRadians");
    public static readonly int waveHeightInPixels = Shader.PropertyToID("_waveHeightInPixels");
    public static readonly int reflectionOffsetInPixels = Shader.PropertyToID("_reflectionOffsetInPixels");
    public static readonly int reflectionLengthInPixels = Shader.PropertyToID("_reflectionLengthInPixels");
    public static readonly int waterLineLengthInPixels = Shader.PropertyToID("_waterLineLengthInPixels");
    public static readonly int tilemapTexture = Shader.PropertyToID("_TilemapTexture");
    public static readonly int sceneTexture = Shader.PropertyToID("_SceneTexture");
    public static readonly int outputTexture = Shader.PropertyToID("_OutputTexture");
    
}
