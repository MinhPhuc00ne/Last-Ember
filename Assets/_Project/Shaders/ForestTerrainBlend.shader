Shader "Antigravity/ForestTerrainBlend"
{
    Properties
    {
        _GrassTex ("Grass Diffuse", 2D) = "white" {}
        _GrassNormal ("Grass Normal", 2D) = "bump" {}
        _DirtTex ("Dirt Path Diffuse", 2D) = "white" {}
        _DirtNormal ("Dirt Path Normal", 2D) = "bump" {}
        _RockTex ("Rock Cliff Diffuse", 2D) = "white" {}
        _RockNormal ("Rock Cliff Normal", 2D) = "bump" {}
        _Tiling ("Texture Tiling", Float) = 70.0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.15
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 tangentWS    : TEXCOORD3;
                float3 bitangentWS  : TEXCOORD4;
                float2 uv           : TEXCOORD5;
                float4 color        : COLOR;
                float fogFactor     : TEXCOORD6;
            };

            CBUFFER_START(UnityPerMaterial)
                float _Tiling;
                float _Smoothness;
            CBUFFER_END

            TEXTURE2D(_GrassTex);       SAMPLER(sampler_GrassTex);
            TEXTURE2D(_GrassNormal);    SAMPLER(sampler_GrassNormal);
            TEXTURE2D(_DirtTex);        SAMPLER(sampler_DirtTex);
            TEXTURE2D(_DirtNormal);     SAMPLER(sampler_DirtNormal);
            TEXTURE2D(_RockTex);        SAMPLER(sampler_RockTex);
            TEXTURE2D(_RockNormal);     SAMPLER(sampler_RockNormal);

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.tangentWS = normalInput.tangentWS;
                output.bitangentWS = normalInput.bitangentWS;
                output.uv = input.uv;
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 scaledUV = input.uv * _Tiling;

                // Sample diffuse textures
                half3 grassCol = SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, scaledUV).rgb;
                half3 dirtCol  = SAMPLE_TEXTURE2D(_DirtTex, sampler_DirtTex, scaledUV).rgb;
                half3 rockCol  = SAMPLE_TEXTURE2D(_RockTex, sampler_RockTex, scaledUV).rgb;

                // Sample normal maps
                half3 grassNorm = UnpackNormal(SAMPLE_TEXTURE2D(_GrassNormal, sampler_GrassNormal, scaledUV));
                half3 dirtNorm  = UnpackNormal(SAMPLE_TEXTURE2D(_DirtNormal, sampler_DirtNormal, scaledUV));
                half3 rockNorm  = UnpackNormal(SAMPLE_TEXTURE2D(_RockNormal, sampler_RockNormal, scaledUV));

                // Vertex color weights: R = Dirt Path, G = Rock Cliff
                float dirtWeight = saturate(input.color.r);
                float rockWeight = saturate(input.color.g);
                float grassWeight = saturate(1.0 - dirtWeight - rockWeight);

                half3 albedo = grassCol * grassWeight + dirtCol * dirtWeight + rockCol * rockWeight;
                half3 normalTS = grassNorm * grassWeight + dirtNorm * dirtWeight + rockNorm * rockWeight;
                normalTS = normalize(normalTS);

                // Transform normal from Tangent space to World space
                float3 sNormalWS = normalize(input.normalWS);
                float3 sTangentWS = normalize(input.tangentWS);
                float3 sBitangentWS = normalize(input.bitangentWS);
                float3x3 tbn = float3x3(sTangentWS, sBitangentWS, sNormalWS);
                float3 finalNormalWS = normalize(mul(normalTS, tbn));

                // Lighting
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half NdotL = saturate(dot(finalNormalWS, mainLight.direction));
                half shadow = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                half3 ambient = SampleSH(finalNormalWS);
                half3 diffuseLight = ambient + mainLight.color * (NdotL * shadow);

                half3 finalColor = albedo * diffuseLight;
                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _MainLightPosition.xyz));
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
