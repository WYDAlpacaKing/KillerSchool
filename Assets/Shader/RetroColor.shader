Shader "Hidden/RetroColor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BrightnessAmount ("Brightness", Float) = 1.0
        _SaturationAmount ("Saturation", Float) = 1.0
        _ContrastAmount ("Contrast", Float) = 1.0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float _BrightnessAmount;
            float _SaturationAmount;
            float _ContrastAmount;

            float3 AdjustContrast(float3 color, float contrast) {
                return (color - 0.5) * contrast + 0.5;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv);

                // 1. 亮度
                col.rgb *= _BrightnessAmount;

                // 2. 饱和度
                float luminance = dot(col.rgb, float3(0.2126, 0.7152, 0.0722));
                col.rgb = lerp(float3(luminance, luminance, luminance), col.rgb, _SaturationAmount);

                // 3. 对比度
                col.rgb = AdjustContrast(col.rgb, _ContrastAmount);

                return col;
            }
            ENDCG
        }
    }
}
