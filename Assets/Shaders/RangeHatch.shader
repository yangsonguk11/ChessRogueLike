Shader "Custom/RangeHatch"
{
    Properties
    {
        _Color          ("Stripe Color",    Color)              = (0.35, 0.75, 1.0, 0.65)
        _BorderColor    ("Border Color",    Color)              = (0.35, 0.75, 1.0, 1.0)
        _LineWidth      ("Stripe Width",    Range(0.01, 0.99))  = 0.35
        _Spacing        ("Stripe Spacing",  Float)              = 0.8
        _Speed          ("Move Speed",      Float)              = 0.6
        _BorderSize     ("Border Size",     Range(0.0, 0.5))    = 0.06
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+1" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Offset -1, -1

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float2 localUV  : TEXCOORD1;
            };

            fixed4 _Color;
            fixed4 _BorderColor;
            float  _LineWidth;
            float  _Spacing;
            float  _Speed;
            float  _BorderSize;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos      = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.localUV  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // ── 테두리 검사 (localUV 0~1 기준) ──────────────────
                float2 uv = i.localUV;
                float borderMask = step(_BorderSize, uv.x)
                                 * step(_BorderSize, uv.y)
                                 * step(_BorderSize, 1.0 - uv.x)
                                 * step(_BorderSize, 1.0 - uv.y);
                // borderMask == 0 이면 테두리 영역

                // ── 빗금 줄무늬 (World XZ 기준 → 인접 타일과 이음매 없음) ──
                float2 wXZ  = i.worldPos.xz;
                // 45° 대각선 방향(+X+Z)으로 이동
                float  diag = (wXZ.x + wXZ.y) / _Spacing + _Time.y * _Speed;
                float  t    = frac(diag);
                float  stripe = step(1.0 - _LineWidth, t);

                // ── 합성 ────────────────────────────────────────────
                fixed4 col;
                if (borderMask < 0.5)
                {
                    // 테두리: 불투명 단색
                    col = _BorderColor;
                }
                else
                {
                    // 내부: 빗금 줄무늬
                    col   = _Color;
                    col.a = _Color.a * stripe;
                }
                return col;
            }
            ENDCG
        }
    }
}
