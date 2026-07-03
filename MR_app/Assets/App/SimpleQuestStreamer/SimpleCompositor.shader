Shader "Custom/SimpleCompositor"
{
    Properties
    {
        _MainTex ("Background (Passthrough)", 2D) = "black" {}
        _ForegroundTex ("Foreground (Unity)", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;      // Video z Passthrough
            sampler2D _ForegroundTex; // Unity Objekty

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 bg = tex2D(_MainTex, i.uv);
                fixed4 fg = tex2D(_ForegroundTex, i.uv);
                
                // Míchání: Vezmi pozadí (bg) a pøelep ho popøedím (fg) podle prùhlednosti (fg.a)
                // Pokud je alpha objektu 0 (prùhledno), bude vidìt video.
                // Pokud je alpha 1 (objekt), bude vidìt objekt.
                return lerp(bg, fg, fg.a);
            }
            ENDCG
        }
    }
}