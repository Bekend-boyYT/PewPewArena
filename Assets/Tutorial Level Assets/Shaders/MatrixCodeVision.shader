Shader "Hidden/PewPewArena/MatrixCodeVision"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "MatrixCodeVision"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            TEXTURE2D_X(_MatrixSourceColor);
            SAMPLER(sampler_MatrixSourceColor);
            TEXTURE2D_X(_MatrixEntityMask);
            SAMPLER(sampler_MatrixEntityMask);
            float4x4 _MatrixInvViewProj;
            float _MatrixReveal;
            float _MatrixRainSpeed;
            float _MatrixDensity;
            float _MatrixNodeDensity;
            float _MatrixEntityBoost;
            float _MatrixFrame;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Segment(float2 p, float2 center, float2 halfSize)
            {
                float2 d = abs(p - center) - halfSize;
                return 1.0 - smoothstep(-0.025, 0.035, max(d.x, d.y));
            }

            float Glyph(float2 p, float seed)
            {
                float a = step(0.48, Hash21(float2(seed, 1.3)));
                float b = step(0.52, Hash21(float2(seed, 2.7)));
                float c = step(0.46, Hash21(float2(seed, 4.1)));
                float d = step(0.58, Hash21(float2(seed, 5.9)));
                float stroke = 0.0;
                stroke = max(stroke, a * Segment(p, float2(0.5, 0.16), float2(0.24, 0.035)));
                stroke = max(stroke, b * Segment(p, float2(0.5, 0.5), float2(0.21, 0.03)));
                stroke = max(stroke, c * Segment(p, float2(0.5, 0.84), float2(0.24, 0.035)));
                stroke = max(stroke, Segment(p, float2(0.23, 0.32), float2(0.035, 0.14)));
                stroke = max(stroke, d * Segment(p, float2(0.77, 0.32), float2(0.035, 0.14)));
                stroke = max(stroke, Segment(p, float2(0.23, 0.68), float2(0.035, 0.14)));
                stroke = max(stroke, Segment(p, float2(0.77, 0.68), float2(0.035, 0.14)));
                return stroke;
            }

            float RainProjection(float2 worldPlane, float axisSeed, float depthFade)
            {
                float scale = _MatrixDensity * depthFade;
                float2 moving = worldPlane * scale;
                moving.y -= _MatrixFrame * _MatrixRainSpeed * lerp(0.28, 1.0, depthFade);
                float2 cell = floor(moving);
                float2 local = frac(moving);
                float columnSeed = Hash21(float2(cell.x + axisSeed, axisSeed * 7.13));
                float glyph = Glyph(local, Hash21(cell + axisSeed));
                float head = frac(-cell.y * 0.071 + _MatrixFrame * _MatrixRainSpeed * 0.09 + columnSeed);
                float tail = exp(-head * lerp(2.8, 5.0, columnSeed));
                float stream = step(0.74, Hash21(float2(cell.x + axisSeed, axisSeed + 9.1)));
                return glyph * stream * tail * (0.38 + 0.62 * columnSeed);
            }

            float WorldRain(float3 worldPosition, float3 normalWS)
            {
                float3 weights = pow(abs(normalWS), 4.0);
                weights /= max(weights.x + weights.y + weights.z, 0.0001);
                float xProjection = RainProjection(worldPosition.zy, 1.7, 0.65);
                float yProjection = RainProjection(worldPosition.xz, 3.9, 0.9);
                float zProjection = RainProjection(worldPosition.xy, 6.2, 1.0);
                return xProjection * weights.x + yProjection * weights.y + zProjection * weights.z;
            }

            float GeometryEdge(float2 uv, float centerDepth, float3 centerNormal)
            {
                float2 texel = _ScreenSize.zw * 1.5;
                float depthX = LinearEyeDepth(SampleSceneDepth(uv + float2(texel.x, 0)), _ZBufferParams);
                float depthY = LinearEyeDepth(SampleSceneDepth(uv + float2(0, texel.y)), _ZBufferParams);
                float eyeDepth = LinearEyeDepth(centerDepth, _ZBufferParams);
                float3 normalX = SampleSceneNormals(uv + float2(texel.x, 0));
                float3 normalY = SampleSceneNormals(uv + float2(0, texel.y));
                float depthEdge = max(abs(depthX - eyeDepth), abs(depthY - eyeDepth)) / max(eyeDepth, 0.1);
                float normalEdge = max(length(normalX - centerNormal), length(normalY - centerNormal));
                return saturate(depthEdge * 18.0 + normalEdge * 1.5);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float rawDepth = SampleSceneDepth(uv);
                float entity = SAMPLE_TEXTURE2D_X(_MatrixEntityMask, sampler_MatrixEntityMask, uv).r;
                float4 scene = SAMPLE_TEXTURE2D_X(_MatrixSourceColor, sampler_MatrixSourceColor, uv);
                float2 revealCell = floor(uv * float2(28.0, 16.0));
                float revealed = step(Hash21(revealCell), _MatrixReveal);
                if (revealed > 0.5) return scene;

                #if UNITY_REVERSED_Z
                    bool sky = rawDepth <= 0.00001;
                #else
                    bool sky = rawDepth >= 0.99999;
                #endif
                if (sky) return float4(0, 0, 0, 1);

                float3 worldPosition = ComputeWorldSpacePosition(uv, rawDepth, _MatrixInvViewProj);
                float3 normalWS = normalize(SampleSceneNormals(uv));
                float rain = WorldRain(worldPosition, normalWS);
                float edge = GeometryEdge(uv, rawDepth, normalWS);
                float3 nodeCell = floor(worldPosition * _MatrixNodeDensity);
                float nodeHash = Hash21(nodeCell);
                float node = step(0.992, nodeHash) * exp(-length(frac(worldPosition * _MatrixNodeDensity) - 0.5) * 9.0);
                float entityCode = saturate(entity) * _MatrixEntityBoost;
                float emission = rain * 1.2 + edge * 1.35 + node * 4.5 + entityCode * 5.5;
                float3 green = float3(0.42, 1.0, 0.54) * emission;
                float3 whiteGreen = float3(1.8, 2.5, 1.9) * saturate(node * 0.8 + entityCode);
                return float4(green + whiteGreen, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "EntityMask"
            Tags { "LightMode" = "UniversalForward" }
            ZTest LEqual
            ZWrite Off
            Cull Back
            ColorMask R

            HLSLPROGRAM
            #pragma vertex MaskVert
            #pragma fragment MaskFrag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct MaskAttributes
            {
                float3 positionOS : POSITION;
            };

            struct MaskVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            MaskVaryings MaskVert(MaskAttributes input)
            {
                MaskVaryings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                return output;
            }

            half4 MaskFrag(MaskVaryings input) : SV_Target
            {
                return half4(1, 1, 1, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
