Shader "PewPew/InvertedHullOutline"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (1, 0.2, 0.2, 1)
        _OutlineWidth("Outline Width", Range(0.0, 10.0)) = 2.0
        _ZOffset("Depth Offset", Range(-0.01, 0.01)) = -0.0002
    }
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Geometry+10"
        }
        LOD 100

        Pass
        {
            Name "InvertedHullOutline"
            Tags { "LightMode" = "UniversalForward" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                float _ZOffset;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                float4 posCS = posInputs.positionCS;
                float3 normCS = TransformWorldToHClipDir(normInputs.normalWS);

                // Extrude in screen space along vertex normal, scaled by posCS.w for perspective stability
                float2 aspect = float2(_ScreenParams.y / max(1.0, _ScreenParams.x), 1.0);
                float2 normalScreen = normalize(normCS.xy + 0.00001);
                float2 offset = normalScreen * (_OutlineWidth * 0.0015) * posCS.w;
                offset.x *= aspect.x;

                posCS.xy += offset;
                posCS.z += _ZOffset * posCS.w;

                output.positionCS = posCS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
