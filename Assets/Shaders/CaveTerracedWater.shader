Shader "Untitled/HD2D/Terraced River Water"
{
    Properties
    {
        _DeepColor("Deep water", Color) = (0.025, 0.13, 0.17, 1)
        _ShallowColor("Shallow water", Color) = (0.09, 0.31, 0.34, 1)
        _FoamColor("Shore ripples", Color) = (0.32, 0.57, 0.55, 1)
        _PixelsPerUnit("Water pixel density", Float) = 32
        _FlowSpeed("Flow speed", Range(0, 2)) = 0.65
        _Smoothness("Smoothness", Range(0, 1)) = 0.86
        _CellSize("Cell size", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Name "WaterForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fog
            #define _SPECULAR_SETUP 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _ShallowColor;
                half4 _FoamColor;
                float _PixelsPerUnit;
                float _FlowSpeed;
                half _Smoothness;
                float _CellSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 banks : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                half4 banks : TEXCOORD2;
                half fog : TEXCOORD3;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                output.banks = input.banks;
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                // World coordinates keep waves continuous across cells and chunk boundaries.
                float2 p = input.positionWS.xz;
                float t = _Time.y * _FlowSpeed;
                // Two low amplitude traveling waves, without pixel snapping or hard white dots.
                float a = p.x * 1.7 + p.y * 2.8 + t;
                float b = p.y * 4.1 - p.x * 2.2 - t * 1.17;
                float wave = sin(a) * 0.6 + sin(b) * 0.4;
                float2 slope = float2(1.02*cos(a)-0.88*cos(b),1.68*cos(a)+1.64*cos(b));
                half3 normalWS = normalize(float3(-slope.x * 0.045, 1, -slope.y * 0.045));
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float surfaceDepth = -TransformWorldToView(input.positionWS).z;
                float depth = max(0, sceneDepth - surfaceDepth);
                float shallow = exp(-depth * 0.85);
                float shoreDistance = min(input.uv.x,1-input.uv.x)*_CellSize;
                float foam = (1-smoothstep(0.02,0.19,shoreDistance))
                    * (0.32+0.16*sin(p.y*2.8-t*1.5));
                float ripple = pow(saturate(wave), 10)*0.12;
                float fresnel = pow(1-saturate(dot(normalWS,GetWorldSpaceNormalizeViewDir(input.positionWS))),5);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = lerp(_DeepColor.rgb, _ShallowColor.rgb, saturate(shallow * 0.65 + wave * 0.08 + 0.08));
                surface.albedo = lerp(surface.albedo, _FoamColor.rgb, saturate(foam + ripple * 0.45));
                surface.specular = half3(0.045, 0.045, 0.045);
                surface.smoothness = _Smoothness;
                surface.normalTS = half3(0, 0, 1);
                surface.occlusion = 1;
                surface.alpha = saturate(0.26 + (1-shallow)*0.53 + fresnel*0.18);
                surface.emission = _ShallowColor.rgb * (0.045 + ripple * 0.08);

                InputData lighting = (InputData)0;
                lighting.positionWS = input.positionWS;
                lighting.normalWS = normalWS;
                lighting.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lighting.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                lighting.bakedGI = SampleSH(normalWS);
                lighting.vertexLighting = VertexLighting(input.positionWS, normalWS);
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                lighting.shadowMask = half4(1, 1, 1, 1);
                half4 color = UniversalFragmentPBR(lighting, surface);
                color.rgb = MixFog(color.rgb, input.fog);
                color.a = surface.alpha;
                return color;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
