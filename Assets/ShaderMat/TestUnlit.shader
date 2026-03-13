Shader "Unlit/TestUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HDR] _Color1("Color 1", Color) = (1.0, 1.0, 1.0)
        [HDR] _Color2("Color 1", Color) = (1.0, 1.0, 1.0)
        
        rectangleMin("Rectangle Min", Vector) = (0.4, 0.4, 0.0, 0.0)
        rectangleMax("Rectangle Max", Vector) = (0.6, 0.6, 0.0, 0.0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            fixed4 _Color1;
            fixed4 _Color2;

            float4 rectangleMin;
            float4 rectangleMax;

            uniform float _oscilationSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float2 GlobalToLocalVect(float2 globalVect, float refFrameAngle)
            {
                float2 refFrameI = float2(cos(refFrameAngle), sin(refFrameAngle));
                float2 refFrameJ = float2(-refFrameI.y, refFrameI.x);
                return float2(dot(globalVect, refFrameI), dot(globalVect, refFrameJ));
            }
            
            float GetDistFromPtToLine(float2 pt, float2 _linePt, float2 _lineDir)
            {
                float2 lineDirNormalized = normalize(_lineDir);
                float2 n = float2(-lineDirNormalized.y, lineDirNormalized.x);
                return dot(pt - _linePt, n);
            }

            float IsInRectangle(float2 pt, float2 min, float2 max)
            {
                return step(pt.x, max.x) * step(pt.y, max.y) * step(min.x, pt.x) * step(min.y, pt.y);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                
                fixed4 r = lerp(_Color1, _Color2, IsInRectangle(i.uv, rectangleMin, rectangleMax));
                UNITY_APPLY_FOG(i.fogCoord, col);
                return r;
            }
            ENDHLSL
        }
    }
}
