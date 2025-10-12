Shader "Tutorial204/BezierBlade"
{
    Properties
    {
        [Header(Shape)]
        _Height ("Height", Float) = 1
        _Tilt ("Tilt", Float) = 0.9
        _BladeWidth ("BladeWidth", Float) = 0.1
        _TaperAmount ("Taper Amount", Float) = 0
        _CurvedNormalAmount ("Curved Normal Amount", Range(0, 5)) = 1
        _p1Offset ("p1Offset", Float) = 1
        _p2Offset ("p2Offset", Float) = 1

        [Header(Shading)]
        _TopColor ("Top Color", Color) = (.25, .5, .5, 1)
        _BottomColor ("Bottom Color", Color) = (.25, .5, .5, 1)
        _GrassAlbedo ("Grass albedo", 2D) = "white" {}
        _GrassGloss ("Grass gloss", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Simple Grass Blade"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back

            HLSLPROGRAM
            // Required to compile gles3.0 on some platforms
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            //阴影
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT

            // Vertex Shader
            #pragma vertex vert
            // Fragment Shader
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "CubicBezier.hlsl"

            float _Height;
            float _Tilt;
            float _BladeWidth;
            float _TaperAmount;
            float _CurvedNormalAmount;
            float _p1Offset;
            float _p2Offset;
            float4 _TopColor;
            float4 _BottomColor;

            TEXTURE2D(_GrassAlbedo);
            SAMPLER(sampler_GrassAlbedo);
            TEXTURE2D(_GrassGloss);
            SAMPLER(sampler_GrassGloss);



            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 curvedNorm : TEXCOORD1;
                float3 originalNorm : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float t : TEXCOORD4;
            };

            float3 GetP0()
            {
                return float3(0,0,0);
            }

            float3 GetP3(float height, float tilt)
            {
                float p3y = tilt * height;
                float p3x = sqrt(height * height - p3y * p3y);
                return float3(-p3x, p3y, 0);
            }

            void GetP1P2(float3 p0, float3 p3, out float3 p1, out float3 p2)
            {
                p1 = lerp(p0, p3, 0.33);
                p2 = lerp(p0, p3, 0.66);

                float3 bladeDir = normalize(p3 - p0);
                float3 bezCtrlOffsetDir = normalize(cross(bladeDir, float3(0,0,1)));

                p1 += bezCtrlOffsetDir * _p1Offset;
                p2 += bezCtrlOffsetDir * _p2Offset;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                //顶点计算
                float3 p0 = GetP0();
                float3 p3 = GetP3(_Height, _Tilt);
                float3 p1 = float3(0,0,0);
                float3 p2 = float3(0,0,0);
                GetP1P2(p0, p3, p1, p2);
                float t = IN.color.r;
                float3 centerPos = CubicBezier(p0, p1, p2, p3, t);
                float width = _BladeWidth * (1 - _TaperAmount * t);
                float side = IN.color.g * 2 - 1;
                float3 vertexPos = centerPos + float3(0, 0, side * width);
                OUT.positionCS = TransformObjectToHClip(vertexPos);

                //切线、法线
                float3 tangent = CubicBezierTangent(p0, p1, p2, p3, t);
                float3 normal = normalize(cross(tangent, float3(0,0,1)));
                //矫正法线
                float3 curvedNorm = normal;
                curvedNorm.z += side * _CurvedNormalAmount;
                curvedNorm = normalize(curvedNorm);
                //转换到世界空间
                OUT.originalNorm = TransformObjectToWorldNormal(normal);
                OUT.curvedNorm = TransformObjectToWorldNormal(curvedNorm);
                OUT.positionWS = TransformObjectToWorld(vertexPos);
                OUT.positionWS = TransformObjectToWorld(vertexPos);
                OUT.uv = IN.texcoord;
                OUT.t = t;

                return OUT;
            }

            
            half4 frag(Varyings i, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                // Calculate normal
                float3 n = isFrontFace ? normalize(i.curvedNorm) : -reflect(-normalize(i.curvedNorm), normalize(i.originalNorm));
                //return half4(n, 1);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float3 v = normalize(GetCameraPositionWS() - i.positionWS);

                float3 grassAlbedo = saturate(SAMPLE_TEXTURE2D(_GrassAlbedo, sampler_GrassAlbedo, i.uv));

                float4 grassCol = lerp(_BottomColor, _TopColor, i.t);

                float3 albedo = grassCol.rgb * grassAlbedo;

                float gloss = (1 - SAMPLE_TEXTURE2D(_GrassGloss, sampler_GrassGloss, i.uv).r) * 0.2;

                half3 GI = SampleSH(n);

                BRDFData brdfData;
                half alpha = 1;

                InitializeBRDFData(albedo, 0, half3(1, 1, 1), gloss, alpha, brdfData);
                float3 directBRDF = DirectBRDF(brdfData, n, mainLight.direction, v) * mainLight.color;
                // Final color calculation
                float3 finalColor = GI * albedo + directBRDF * (mainLight.shadowAttenuation * mainLight.distanceAttenuation);
                float4 col;
                col = float4(finalColor, grassCol.a); // Alpha from grassCol

                return half4(col);
            }

            ENDHLSL
        }
    }
}