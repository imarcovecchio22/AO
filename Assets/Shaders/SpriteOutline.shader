// Sprite outline para Unity 6 / URP.
// Usa CG (no HLSL con CBUFFER) para que SpriteRenderer pueda setear
// _MainTex y _Color via MaterialPropertyBlock correctamente.
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
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
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

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _MainTex_TexelSize;
            fixed4    _OutlineColor;
            float     _OutlineSize;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 ts = _MainTex_TexelSize.xy * _OutlineSize;

                float a = tex2D(_MainTex, i.uv + float2( ts.x,  0   )).a
                        + tex2D(_MainTex, i.uv + float2(-ts.x,  0   )).a
                        + tex2D(_MainTex, i.uv + float2( 0,     ts.y)).a
                        + tex2D(_MainTex, i.uv + float2( 0,    -ts.y)).a;
                a = saturate(a);

                // No dibujar donde el propio sprite es opaco
                float selfA = tex2D(_MainTex, i.uv).a;
                a *= (1.0 - selfA);

                fixed4 col = _OutlineColor;
                col.a *= a;
                return col;
            }
            ENDCG
        }

        // ── Pasada 2: sprite normal ───────────────────────────────────────────
        Pass
        {
            Name "Sprite"

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
                o.color  = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * i.color;
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
