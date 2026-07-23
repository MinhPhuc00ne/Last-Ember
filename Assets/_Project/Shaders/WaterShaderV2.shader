Shader "Antigravity/WaterShaderV2"
{
    Properties
    {
        [Header(Water Colors)]
        _BaseColor ("Base Water Color", Color) = (0.08, 0.28, 0.38, 0.75)
        _DeepWaterColor ("Deep Water Color", Color) = (0.02, 0.10, 0.20, 0.92)
        _ShallowWaterColor ("Shallow Water Color", Color) = (0.18, 0.55, 0.60, 0.45)
        _FoamColor ("Foam Color", Color) = (0.92, 0.96, 1.0, 0.85)

        [Header(Normal Map & Animation)]
        _BumpMap ("Normal Map 1", 2D) = "bump" {}
        _BumpMap2 ("Normal Map 2", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 2)) = 0.65
        _Wave1Speed ("Wave 1 Scroll (XY)", Vector) = (0.03, 0.02, 0, 0)
        _Wave2Speed ("Wave 2 Scroll (XY)", Vector) = (-0.02, 0.035, 0, 0)

        [Header(Wave Displacement)]
        _WaveAmplitude ("Wave Amplitude", Range(0, 1)) = 0.12
        _WaveFrequency ("Wave Frequency", Range(0.1, 5)) = 1.2
        _WaveSpeed ("Wave Animation Speed", Range(0.1, 5)) = 1.0

        [Header(Lighting & Reflection)]
        _Smoothness ("Smoothness", Range(0, 1)) = 0.92
        _Metallic ("Metallic", Range(0, 1)) = 0.08
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 3.5

        [Header(Shoreline Foam & Depth)]
        _FoamDistance ("Foam Shoreline Distance", Range(0.01, 5)) = 1.5
        _FoamCutoff ("Foam Threshold", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
        }

        LOD 300
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
                float3 tangentWS    : TEXCOORD3;
                float3 bitangentWS  : TEXCOORD4;
                float4 screenPos    : TEXCOORD5;
                float fogFactor     : TEXCOORD6;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _DeepWaterColor;
                float4 _ShallowWaterColor;
                float4 _FoamColor;
                float4 _BumpMap_ST;
                float4 _BumpMap2_ST;
                float4 _Wave1Speed;
                float4 _Wave2Speed;
                float _NormalStrength;
                float _WaveAmplitude;
                float _WaveFrequency;
                float _WaveSpeed;
                float _Smoothness;
                float _Metallic;
                float _FresnelPower;
                float _FoamDistance;
                float _FoamCutoff;
            CBUFFER_END

            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_BumpMap2);
            SAMPLER(sampler_BumpMap2);

            // Gerstner Wave calculation for organic surface wave movement
            float3 CalculateGerstnerWave(float3 pos, float2 direction, float steepness, float wavelength, float speed, inout float3 normal)
            {
                float k = 2.0 * 3.14159265 / wavelength;
                float c = sqrt(9.8 / k);
                float2 d = normalize(direction);
                float f = k * (dot(d, pos.xz) - c * speed * _Time.y * _WaveSpeed);
                float a = steepness / k;

                normal += float3(
                    -d.x * (a * k * cos(f)),
                    1.0 - (a * k * sin(f)),
                    -d.y * (a * k * cos(f))
                );

                return float3(
                    d.x * (a * cos(f)),
                    a * sin(f),
                    d.y * (a * cos(f))
                );
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 waveNormal = float3(0, 0, 0);

                // Combine 3 Gerstner Wave components
                float3 waveOffset = float3(0, 0, 0);
                waveOffset += CalculateGerstnerWave(posWS, float2(1.0, 0.3), _WaveAmplitude * 0.4, 6.0 / _WaveFrequency, 1.2, waveNormal);
                waveOffset += CalculateGerstnerWave(posWS, float2(0.3, 1.0), _WaveAmplitude * 0.3, 3.5 / _WaveFrequency, 1.5, waveNormal);
                waveOffset += CalculateGerstnerWave(posWS, float2(-0.7, 0.7), _WaveAmplitude * 0.2, 2.0 / _WaveFrequency, 1.8, waveNormal);

                posWS += waveOffset;

                output.positionCS = TransformWorldToHClip(posWS);
                output.positionWS = posWS;
                output.uv = TRANSFORM_TEX(input.uv, _BumpMap);

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = normalize(lerp(normalInput.normalWS, waveNormal, 0.5));
                output.tangentWS = normalInput.tangentWS;
                output.bitangentWS = normalInput.bitangentWS;

                output.screenPos = ComputeScreenPos(output.positionCS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv1 = input.uv + _Wave1Speed.xy * _Time.y;
                float2 uv2 = input.uv * 1.35 + _Wave2Speed.xy * _Time.y;

                // Sample dual scrolling normal maps
                float3 n1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv1), _NormalStrength);
                float3 n2 = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap2, sampler_BumpMap2, uv2), _NormalStrength);
                float3 normalTS = normalize(n1 + n2);

                // Transform normal from Tangent Space to World Space
                float3x3 tbn = float3x3(input.tangentWS, input.bitangentWS, input.normalWS);
                float3 normalWS = normalize(mul(normalTS, tbn));

                float3 viewDirWS = SafeNormalize(GetCameraPositionWS() - input.positionWS);

                // Depth & Shoreline Foam calculation
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                #if defined(REQUIRE_DEPTH_TEXTURE)
                    float rawDepth = SampleSceneDepth(screenUV);
                    float sceneEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                    float surfaceEyeDepth = input.screenPos.w;
                    float depthDiff = max(0.0, sceneEyeDepth - surfaceEyeDepth);
                #else
                    float depthDiff = 1.0;
                #endif

                // Water Color Depth Blend (Shallow -> Base -> Deep)
                float depthFactor = saturate(depthDiff / 4.0);
                half4 waterColor = lerp(_ShallowWaterColor, lerp(_BaseColor, _DeepWaterColor, depthFactor), saturate(depthDiff / 12.0));

                // Shoreline Foam Blend
                float foamFactor = saturate(1.0 - (depthDiff / max(0.01, _FoamDistance)));
                foamFactor = smoothstep(_FoamCutoff, 1.0, foamFactor);
                waterColor = lerp(waterColor, _FoamColor, foamFactor * _FoamColor.a);

                // Fresnel Reflection
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);
                waterColor.rgb += fresnel * 0.25;

                // Lighting & Specular Highlights
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 lightDirWS = normalize(mainLight.direction);
                float NdotL = saturate(dot(normalWS, lightDirWS));

                // Blinn-Phong Specular Reflection
                float3 halfDir = normalize(lightDirWS + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specPower = exp2(_Smoothness * 10.0 + 1.0);
                float specular = pow(NdotH, specPower) * _Smoothness * mainLight.shadowAttenuation;

                float3 finalColor = waterColor.rgb * (mainLight.color * (NdotL * 0.6 + 0.4)) + mainLight.color * specular;

                // Apply Fog
                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, waterColor.a);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
