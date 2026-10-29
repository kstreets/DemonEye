// Stamps a sprite's alpha into the blood mask render texture.
// Positions are mapped straight from world space into the mask's clip space using _BloodMaskRect,
// so no camera or view projection matrices are needed when drawing from a command buffer.
Shader "Hidden/BloodStamp" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader {
        Cull Off
        ZWrite Off
        ZTest Always
        BlendOp Max
        Blend One One
        ColorMask R

        Pass {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
            CBUFFER_END

            // xy = mask world min, zw = 1 / mask world size
            float4 _BloodMaskRect;

            Varyings Vert(Attributes input) {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float2 maskUV = (worldPos.xy - _BloodMaskRect.xy) * _BloodMaskRect.zw;
                float2 clipPos = maskUV * 2.0 - 1.0;
                #if UNITY_UV_STARTS_AT_TOP
                    clipPos.y = -clipPos.y;
                #endif
                output.positionCS = float4(clipPos, 0.5, 1.0);
                output.uv = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
            }
            ENDHLSL
        }
    }
}
