// Shader URP para sprites 2D con contorno (outline) de 1 texel.
// Compatible con Unity 6 / URP 17+.
// Aplica dos pasadas: primero dibuja el outline, luego el sprite encima.
Shader "Custom/SpriteOutline"
{
    Properties
    {
        _MainTex      ("Sprite Texture", 2D)    = "white" {}
        [PerRendererData] _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color)  = (0,0,0,0.85)
        _OutlineSize  ("Outline Size (texels)", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType"     = "Plane"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        // ── Pasada 1: outline ─────────────────────────────────────────────────
        Pass
        {
            Name "Outline"

            HLSLPROGRAM
            #pragma vertex   VertOutline
            #pragma fragment FragOutline

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;  // (.xy=1/wh, .zw=wh)
                half4  _OutlineColor;
                float  _OutlineSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings VertOutline(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 FragOutline(Varyings IN) : SV_Target
            {
                float2 ts = _MainTex_TexelSize.xy * _OutlineSize;

                // Muestrear los 4 vecinos cardinales
                float a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( ts.x,  0   )).a
                        + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(-ts.x,  0   )).a
                        + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( 0,     ts.y)).a
                        + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( 0,    -ts.y)).a;
                a = saturate(a);

                // Sólo dibujar donde el propio sprite es transparente
                float selfA = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a;
                a *= (1.0 - selfA);

                half4 col = _OutlineColor;
                col.a *= a;
                return col;
            }
            ENDHLSL
        }

        // ── Pasada 2: sprite normal ───────────────────────────────────────────
        Pass
        {
            Name "Sprite"

            HLSLPROGRAM
            #pragma vertex   VertSprite
            #pragma fragment FragSprite

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                half4  color       : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings VertSprite(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv    = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 FragSprite(Varyings IN) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
            }
            ENDHLSL
        }
    }

    FallBack "Sprites/Default"
}
