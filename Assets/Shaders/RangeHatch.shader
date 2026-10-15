// 보드 칸 범위 표시(아군/적/시전자) — 칸마다 쿼드 하나에 그린다.
// - 줄무늬는 월드 XZ 기준이라 붙은 칸끼리 이음매 없이 이어진다.
// - 테두리는 칸 단위가 아니라 "범위 영역의 바깥쪽"에만 그린다. 어느 변·모서리에 그릴지는 Board.RefreshRangeDisplay가
//   이웃 칸 상태로 계산해 MaterialPropertyBlock(_EdgeMask/_CornerMask)으로 넘긴다(Button.SetRangeVisual).
// - 텍스트(월드 캔버스, Transparent=3000)보다 먼저 그려지도록 Transparent-1에 둔다.
Shader "Custom/RangeHatch"
{
    Properties
    {
        _Color          ("Stripe Color",    Color)              = (0.35, 0.75, 1.0, 0.65)
        _BorderColor    ("Border Color",    Color)              = (0.35, 0.75, 1.0, 1.0)
        _LineWidth      ("Stripe Width",    Range(0.01, 0.99))  = 0.35
        _Spacing        ("Stripe Spacing",  Float)              = 0.8
        _Speed          ("Move Speed",      Float)              = 0.6
        _StripeDirection("Stripe Direction (1 / -1)", Float)    = 1
        _BorderSize     ("Border Size",     Range(0.0, 0.5))    = 0.06

        // 비트: 1=+X 2=-X 4=+Z 8=-Z 변에 테두리. 기본 15 = 네 변 모두(마스크를 안 받은 단독 칸).
        [HideInInspector] _EdgeMask   ("Edge Mask",   Float) = 15
        // 비트: 1=(+X,+Z) 2=(-X,+Z) 4=(+X,-Z) 8=(-X,-Z) 오목한 모서리.
        [HideInInspector] _CornerMask ("Corner Mask", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent-1"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "RangeHatch"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _BorderColor;
                float _LineWidth;
                float _Spacing;
                float _Speed;
                float _StripeDirection;
                float _BorderSize;
                float _EdgeMask;
                float _CornerMask;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 centerWS   : TEXCOORD1;
                float  cellSize   : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                // 칸 중심과 한 변의 월드 길이(쿼드는 로컬 1x1) — 호버 스케일이 바뀌어도 칸 안 좌표는 -0.5~0.5로 유지된다.
                output.centerWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                output.cellSize = length(TransformObjectToWorldDir(float3(1.0, 0.0, 0.0), false));
                return output;
            }

            // 정수값이 들어 있는 float 마스크에서 i번째 비트(0/1).
            float Bit(float mask, float i)
            {
                return fmod(floor((mask + 0.5) / exp2(i)), 2.0);
            }

            // 변까지의 거리 d가 테두리 두께 안이면 1(경계는 화면 픽셀 크기 aa만큼 부드럽게).
            float Band(float d, float width, float aa)
            {
                return 1.0 - smoothstep(width - aa, width + aa, d);
            }

            half4 frag(Varyings input) : SV_Target
            {
                // ── 테두리 ─────────────────────────────────────────
                // 칸 중심 기준 월드 XZ 좌표(-0.5~0.5). 보드 x+1 = 월드 +X, 보드 y+1 = 월드 +Z.
                float2 local = (input.positionWS.xz - input.centerWS.xz) / max(input.cellSize, 1e-4);
                float aa = max(fwidth(local.x), fwidth(local.y));

                float bandPX = Band(0.5 - local.x, _BorderSize, aa); // +X 변
                float bandNX = Band(0.5 + local.x, _BorderSize, aa); // -X 변
                float bandPZ = Band(0.5 - local.y, _BorderSize, aa); // +Z 변
                float bandNZ = Band(0.5 + local.y, _BorderSize, aa); // -Z 변

                float border = 0.0;
                border = max(border, Bit(_EdgeMask, 0) * bandPX);
                border = max(border, Bit(_EdgeMask, 1) * bandNX);
                border = max(border, Bit(_EdgeMask, 2) * bandPZ);
                border = max(border, Bit(_EdgeMask, 3) * bandNZ);

                // 오목한 모서리: 두 변 쪽 이웃은 범위 안인데 그 사이 대각선 칸만 밖일 때, 바깥 테두리가 끊기지 않게 모서리만 채운다.
                border = max(border, Bit(_CornerMask, 0) * min(bandPX, bandPZ));
                border = max(border, Bit(_CornerMask, 1) * min(bandNX, bandPZ));
                border = max(border, Bit(_CornerMask, 2) * min(bandPX, bandNZ));
                border = max(border, Bit(_CornerMask, 3) * min(bandNX, bandNZ));

                // ── 빗금 줄무늬 (월드 XZ 기준 → 인접 칸과 이음매 없음) ──
                // _StripeDirection을 -1로 두면 반대 대각선 — 아군/적 범위가 겹친 칸이 격자무늬로 구분된다.
                float diag = (input.positionWS.x * _StripeDirection + input.positionWS.z) / _Spacing + _Time.y * _Speed;
                float stripeAA = fwidth(diag);
                float stripe = Band(abs(frac(diag) - 0.5), _LineWidth * 0.5, stripeAA);

                half4 fill = half4(_Color.rgb, _Color.a * stripe);
                return lerp(fill, _BorderColor, border);
            }
            ENDHLSL
        }
    }
}
