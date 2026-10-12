Shader "Untitled/HD2D/Terraced Pixel Terrain"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        _TextureStrength("Texture Contrast", Range(0,1)) = 1
        _TileScale("World Texture Scale", Float) = 1
        _PixelDensity("World pixels per metre", Float) = 12
        _PaletteSteps("Color steps", Float) = 24
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5
        _Smoothness("Smoothness", Range(0,1)) = 0
        _Metallic("Metallic", Range(0,1)) = 0
        _MetallicGlossMap("Metallic", 2D) = "white" {}
        _SpecColor("Specular", Color) = (0.2,0.2,0.2,1)
        _SpecGlossMap("Specular", 2D) = "white" {}
        _BumpMap("Normal", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1
        _OcclusionMap("Occlusion", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0,1)) = 1
        _EmissionMap("Emission", 2D) = "white" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)
        [HideInInspector] _Surface("Surface", Float) = 0
        [HideInInspector] _Cull("Cull", Float) = 2
        [HideInInspector] _AlphaClip("Alpha Clip", Float) = 0
        [HideInInspector] _ReceiveShadows("Receive Shadows", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "UniversalMaterialType"="Lit" }
        Cull [_Cull]
        ZWrite On

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"

        // Globals belong to the scene controller, not the shared material asset.
        float4 _CaveRevealTarget;
        float4 _CaveRevealCamera;
        float _TextureStrength;
        float _TileScale;
        float _PixelDensity;
        float _PaletteSteps;
        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
            half4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            float2 uv : TEXCOORD2;
            half wall : TEXCOORD3;
            half fog : TEXCOORD4;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };
        Varyings Vert(Attributes input)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
            output.wall = input.color.r;
            output.fog = ComputeFogFactor(output.positionCS.z);
            return output;
        }
        void Reveal(Varyings input)
        {
            if (input.wall < 0.5 || _CaveRevealCamera.w <= 0 || _CaveRevealTarget.w <= 0) return;
            // Scene View and other cameras retain intact walls.
            if (distance(_WorldSpaceCameraPos, _CaveRevealCamera.xyz) > 0.01) return;
            float targetDepth = -TransformWorldToView(_CaveRevealTarget.xyz).z;
            float wallDepth = -TransformWorldToView(input.positionWS).z;
            if (targetDepth <= 0 || wallDepth >= targetDepth - 0.05) return;
            float4 targetCS = TransformWorldToHClip(_CaveRevealTarget.xyz);
            float4 wallCS = TransformWorldToHClip(input.positionWS);
            // Project both positions in the same convention; no CPU viewport/Y-flip mismatch.
            float2 delta = (wallCS.xy / wallCS.w - targetCS.xy / targetCS.w) * 0.5;
            delta.x *= _ScaledScreenParams.x / _ScaledScreenParams.y;
            float radius = _CaveRevealTarget.w * abs(UNITY_MATRIX_P._m11) * 0.5;
            if (unity_OrthoParams.w < 0.5) radius /= targetDepth;
            float mask = (1 - smoothstep(radius * 0.75, radius, length(delta))) * _CaveRevealCamera.w;
            // Ordered dither keeps opaque depth sorting, with a soft reveal edge/fade.
            static const float4 rows[4] = {
                float4(0,8,2,10), float4(12,4,14,6), float4(3,11,1,9), float4(15,7,13,5)
            };
            uint2 pixel = (uint2)input.positionCS.xy;
            float threshold = (rows[pixel.y & 3][pixel.x & 3] + 0.5) / 16;
            clip(1 - mask - threshold);
        }
        half4 ForwardFrag(Varyings input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            Reveal(input);
            SurfaceData surface;
            // World projection keeps one source tile per metre on floors, cliffs and scaled architecture.
            float3 axis = abs(input.normalWS);
            float2 tileUV = axis.y > max(axis.x, axis.z) ? input.positionWS.xz
                : axis.x > axis.z ? input.positionWS.zy : input.positionWS.xy;
            // A single world pixel grid is used on floor, wall and scaled meshes.
            // Distance reduces fine contrast while nearby pixels remain crisp.
            float2 pixelUV = (floor(tileUV * _PixelDensity) + 0.5) / _PixelDensity;
            InitializeStandardLitSurfaceData(frac(pixelUV * _TileScale) * _BaseMap_ST.xy + _BaseMap_ST.zw, surface);
            surface.albedo = floor(surface.albedo * _PaletteSteps + 0.5) / _PaletteSteps;
            half3 meanColor = (
                SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, _BaseMap_ST.zw + _BaseMap_ST.xy * float2(.2,.2)).rgb +
                SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, _BaseMap_ST.zw + _BaseMap_ST.xy * float2(.7,.2)).rgb +
                SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, _BaseMap_ST.zw + _BaseMap_ST.xy * float2(.2,.7)).rgb +
                SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, _BaseMap_ST.zw + _BaseMap_ST.xy * float2(.7,.7)).rgb
                ) * .25 * _BaseColor.rgb;
            float distant = smoothstep(28,100,distance(input.positionWS,_WorldSpaceCameraPos));
            surface.albedo = lerp(surface.albedo,meanColor,distant*0.65);
            surface.albedo = lerp(meanColor, surface.albedo, _TextureStrength);
            InputData lighting = (InputData)0;
            lighting.positionWS = input.positionWS;
            lighting.normalWS = NormalizeNormalPerPixel(input.normalWS);
            lighting.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
            lighting.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
            lighting.bakedGI = SampleSH(lighting.normalWS);
            lighting.vertexLighting = VertexLighting(input.positionWS, lighting.normalWS);
            lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
            lighting.shadowMask = half4(1,1,1,1);
            half4 color = UniversalFragmentPBR(lighting, surface);
            color.rgb = MixFog(color.rgb, input.fog);
            return color;
        }
        half4 DepthFrag(Varyings input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            Reveal(input);
            return input.positionCS.zzzz;
        }
        half4 NormalsFrag(Varyings input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            Reveal(input);
            half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
            #if defined(_GBUFFER_NORMALS_OCT)
                float2 oct = PackNormalOctQuadEncode(normalWS) * 0.5 + 0.5;
                return half4(PackFloat2To888(oct), 0);
            #else
                return half4(normalWS, 0);
            #endif
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment ForwardFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _RECEIVE_SHADOWS_OFF
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment NormalsFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing
            ENDHLSL
        }
        // A camera reveal is not a physical hole: light shadows and baked geometry stay intact.
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/Meta"
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
