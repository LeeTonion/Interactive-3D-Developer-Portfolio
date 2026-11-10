Shader "SpaceCyberpunk/SciFiHexShieldDissolve"
{
    Properties
    {
        [Header(Shield Visual Colors)]
        [HDR] _BaseColor ("Shield Base Color", Color) = (0.02, 0.35, 0.65, 0.12)
        [HDR] _RimColor ("Rim Fresnel Glow (HDR)", Color) = (0.4, 1.6, 3.2, 1.0)
        _RimPower ("Rim Fresnel Power", Range(0.5, 6.0)) = 2.4
        
        [Header(Hexagonal Grid Pattern)]
        _HexTex ("Hexagon Honeycomb Texture", 2D) = "white" {}
        _HexScale ("Hex Grid Scale", Float) = 8.0
        [HDR] _HexLineColor ("Hex Line Glow (HDR)", Color) = (0.35, 2.0, 3.6, 1.0)
        _HexLineGlow ("Hex Line Intensity", Float) = 2.2
        _HexInnerFill ("Hex Center Glow", Range(0.0, 0.5)) = 0.08

        [Header(Dissolve Effect)]
        _DissolveAmount ("Dissolve Progress (0=Intact, 1=Dissolved)", Range(0.0, 1.0)) = 0.0
        _DissolveNoiseScale ("Dissolve Noise Scale", Float) = 2.4
        _DissolveEdgeWidth ("Dissolve Edge Width", Range(0.01, 0.35)) = 0.09
        [HDR] _DissolveEdgeColor ("Dissolve Edge Glow (HDR)", Color) = (0.4, 2.5, 4.8, 2.5)
        _DissolveEdgeGlow ("Dissolve Edge Intensity", Float) = 3.5

        [Header(Combat Impact Reaction)]
        _HitPos ("Hit Impact Local Pos", Vector) = (0, 0, 0, 0)
        _HitIntensity ("Hit Flash Intensity (0=Stealth Invisible)", Range(0.0, 1.0)) = 0.0
        _HitWaveProgress ("Hit Ripple Radius", Float) = 0.0
        [HDR] _HitColor ("Hit Ripple Color (HDR - Magenta/Cyan)", Color) = (1.2, 0.25, 0.75, 2.5)
        _HitImpactRadius ("Hit Impact Radius", Float) = 5.2
    }

    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent+250" 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Pass
        {
            Name "HexShieldForward"
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float2 uv         : TEXCOORD3;
            };

            TEXTURE2D(_HexTex);
            SAMPLER(sampler_HexTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float _RimPower;
                float4 _HexTex_ST;
                float _HexScale;
                float4 _HexLineColor;
                float _HexLineGlow;
                float _HexInnerFill;
                float _DissolveAmount;
                float _DissolveNoiseScale;
                float _DissolveEdgeWidth;
                float4 _DissolveEdgeColor;
                float _DissolveEdgeGlow;
                float4 _HitPos;
                float _HitIntensity;
                float _HitWaveProgress;
                float4 _HitColor;
                float _HitImpactRadius;
            CBUFFER_END

            // 3D Noise for organic Dissolve effect
            float3 Hash33(float3 p3)
            {
                p3 = frac(p3 * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yxz + 33.33);
                return frac((p3.xxy + p3.yxx) * p3.zyx);
            }

            float GradientNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);

                float n000 = dot(Hash33(i + float3(0,0,0)) - 0.5, f - float3(0,0,0));
                float n100 = dot(Hash33(i + float3(1,0,0)) - 0.5, f - float3(1,0,0));
                float n010 = dot(Hash33(i + float3(0,1,0)) - 0.5, f - float3(0,1,0));
                float n110 = dot(Hash33(i + float3(1,1,0)) - 0.5, f - float3(1,1,0));
                float n001 = dot(Hash33(i + float3(0,0,1)) - 0.5, f - float3(0,0,1));
                float n101 = dot(Hash33(i + float3(1,0,1)) - 0.5, f - float3(1,0,1));
                float n011 = dot(Hash33(i + float3(0,1,1)) - 0.5, f - float3(0,1,1));
                float n111 = dot(Hash33(i + float3(1,1,1)) - 0.5, f - float3(1,1,1));

                float n00 = lerp(n000, n100, u.x);
                float n10 = lerp(n010, n110, u.x);
                float n01 = lerp(n001, n101, u.x);
                float n11 = lerp(n011, n111, u.x);

                float n0 = lerp(n00, n10, u.y);
                float n1 = lerp(n01, n11, u.y);

                return lerp(n0, n1, u.z) * 1.4 + 0.5;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.positionOS = input.positionOS.xyz;
                output.normalWS   = normalInput.normalWS;
                output.uv         = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float dissolveThreshold = _DissolveAmount;
                float hitIntensity = _HitIntensity;

                // TÀNG HÌNH (STEALTH): Khi không bị bắn VÀ không trong trạng thái vỡ khiên -> Ẩn hoàn toàn!
                if (dissolveThreshold <= 0.001 && hitIntensity <= 0.001)
                {
                    discard;
                }

                // 1. DISSOLVE EFFECT (Khi khiên vỡ hoặc tái sinh)
                float noiseVal = GradientNoise3D(input.positionOS * _DissolveNoiseScale);
                float noiseDetail = GradientNoise3D(input.positionOS * (_DissolveNoiseScale * 2.3));
                float combinedNoise = saturate(noiseVal * 0.72 + noiseDetail * 0.28);

                if (dissolveThreshold >= 0.999)
                {
                    discard;
                }

                float dissolveMask = combinedNoise - dissolveThreshold;
                if (dissolveMask < 0.0)
                {
                    discard;
                }

                // 2. FRESNEL / RIM
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);
                float3 normalWS = normalize(input.normalWS);
                if (dot(normalWS, viewDirWS) < 0.0)
                {
                    normalWS = -normalWS;
                }

                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, _RimPower);

                // 3. HEXAGONAL HONEYCOMB TEXTURE SAMPLE (Triplanar)
                float3 nAbs = pow(abs(normalWS), 3.0);
                float3 weights = nAbs / (nAbs.x + nAbs.y + nAbs.z + 0.0001);

                float2 uvX = input.positionOS.zy * (_HexScale * 0.2);
                float2 uvY = input.positionOS.xz * (_HexScale * 0.2);
                float2 uvZ = input.positionOS.xy * (_HexScale * 0.2);

                float hexValX = SAMPLE_TEXTURE2D(_HexTex, sampler_HexTex, uvX).a;
                float hexValY = SAMPLE_TEXTURE2D(_HexTex, sampler_HexTex, uvY).a;
                float hexValZ = SAMPLE_TEXTURE2D(_HexTex, sampler_HexTex, uvZ).a;
                float hexLine = hexValX * weights.x + hexValY * weights.y + hexValZ * weights.z;

                // 4. COMBAT HIT LOCALIZED FLASH (Hiệu ứng ảnh 1: Chỉ bừng sáng cục bộ tại vùng trúng đạn)
                float hitDist = length(input.positionOS - _HitPos.xyz);
                float hitMask = saturate(1.0 - hitDist / max(_HitImpactRadius, 1.0));
                hitMask = pow(hitMask, 1.3);

                // Sóng năng lượng xung kích lan truyền từ điểm chạm
                float ripple = sin(saturate(1.0 - abs(hitDist - _HitWaveProgress) / 1.6) * 3.1415926);
                float hitActivation = hitIntensity * saturate(hitMask * 0.75 + ripple * 0.95);

                // 5. COLOR COMPOSITION (Màu hồng tím Magenta / Cyan giống ảnh 1)
                float3 hitColorHDR = _HitColor.rgb * (hexLine * _HexLineGlow * 1.5 + _HexInnerFill);
                float3 rimCol = _RimColor.rgb * fresnel;
                float3 finalColor = (hitColorHDR + rimCol * 0.6) * hitActivation;
                float masterAlpha = saturate(hitActivation * (fresnel * 0.65 + hexLine * 0.85 + 0.2));

                // 6. DISSOLVE GLOWING BURN EDGE
                if (dissolveThreshold > 0.001)
                {
                    finalColor += _HexLineColor.rgb * (hexLine * saturate(1.0 - dissolveThreshold));
                    masterAlpha = max(masterAlpha, saturate((1.0 - dissolveThreshold) * 0.6));

                    if (dissolveMask < _DissolveEdgeWidth)
                    {
                        float edgeProgress = 1.0 - (dissolveMask / _DissolveEdgeWidth);
                        float edgeGlow = pow(edgeProgress, 2.0) * _DissolveEdgeGlow;
                        finalColor += _DissolveEdgeColor.rgb * edgeGlow;
                        masterAlpha = saturate(masterAlpha + edgeProgress);
                    }
                }

                if (masterAlpha <= 0.005)
                {
                    discard;
                }

                return half4(finalColor, masterAlpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
