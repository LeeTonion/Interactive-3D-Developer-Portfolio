Shader "SpaceCyberpunk/HologramPlanetShader"
{
    Properties
    {
        [Header(Hologram Colors)]
        _MainColor ("Base Ocean Color", Color) = (0.02, 0.25, 0.55, 0.25)
        [HDR] _RimColor ("Fresnel Rim Glow (HDR)", Color) = (0.1, 0.95, 1.0, 2.0)
        [HDR] _GridColor ("Grid & Continent Glow (HDR)", Color) = (0.35, 1.0, 1.0, 2.2)
        [HDR] _EquatorColor ("Equator Ring Glow (HDR)", Color) = (0.6, 1.0, 1.0, 2.5)

        [Header(Fresnel Edge Effect)]
        _FresnelPower ("Fresnel Rim Power", Float) = 2.4
        _InnerAlpha ("Inner Transparency", Range(0, 1)) = 0.18

        [Header(Scanlines)]
        _ScanlineFreq ("Scanline Frequency", Float) = 75.0
        _ScanlineSpeed ("Scanline Scroll Speed", Float) = 3.0
        _ScanlineStrength ("Scanline Intensity", Range(0, 1)) = 0.50

        [Header(Coordinate Grid)]
        _GridLinesLat ("Latitude Lines", Float) = 14.0
        _GridLinesLon ("Longitude Lines", Float) = 20.0
        _GridSharpness ("Grid Sharpness", Float) = 40.0

        [Header(Continent Map)]
        _ContinentScale ("Continent Noise Scale", Float) = 4.5
        _CoastlineGlow ("Coastline Edge Glow", Float) = 1.8

        [Header(Glitch and Animation)]
        _GlitchFrequency ("Glitch Frequency", Float) = 6.0
        _GlitchStrength ("Glitch Vertex Jitter", Float) = 0.025
        _FlickerSpeed ("CRT Flicker Speed", Float) = 18.0
        _PulseSpeed ("Vertical Radar Pulse Speed", Float) = 1.2
        _Alpha ("Master Hologram Alpha", Range(0, 1)) = 0.95
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
            Name "HologramPlanetForward"
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
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainColor;
                float4 _RimColor;
                float4 _GridColor;
                float4 _EquatorColor;
                float _FresnelPower;
                float _InnerAlpha;
                float _ScanlineFreq;
                float _ScanlineSpeed;
                float _ScanlineStrength;
                float _GridLinesLat;
                float _GridLinesLon;
                float _GridSharpness;
                float _ContinentScale;
                float _CoastlineGlow;
                float _GlitchFrequency;
                float _GlitchStrength;
                float _FlickerSpeed;
                float _PulseSpeed;
                float _Alpha;
            CBUFFER_END

            // Fast pseudo-random hash for continent shapes
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Smooth 2D value noise
            float valueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = hash21(i + float2(0.0, 0.0));
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Fractal noise for organic landmasses
            float fbm(float2 uv)
            {
                float v = 0.0;
                float a = 0.5;
                float2 shift = float2(100.0, 100.0);
                for (int i = 0; i < 4; ++i)
                {
                    v += a * valueNoise(uv);
                    uv = uv * 2.0 + shift;
                    a *= 0.5;
                }
                return v;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Subtle Hologram Glitch Displace
                float glitchBlock = floor(_Time.y * _GlitchFrequency);
                float glitchRand = hash21(float2(glitchBlock, floor(input.positionOS.y * 8.0)));
                float3 pos = input.positionOS.xyz;
                if (glitchRand > 0.88)
                {
                    pos.x += sin(glitchRand * 45.0) * _GlitchStrength;
                    pos.z += cos(glitchRand * 33.0) * _GlitchStrength;
                }

                VertexPositionInputs vertexInput = GetVertexPositionInputs(pos);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS   = normalInput.normalWS;
                output.uv         = input.uv;
                return output;
            }

            half4 frag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);
                float3 normalWS = normalize(input.normalWS);
                if (!isFrontFace) normalWS = -normalWS; // 2-sided normal for Cull Off

                // 1. Fresnel Rim Effect (The signature Shader Graph Hologram edge glow)
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);

                // 2. Scanlines (Horizontal laser interference bands)
                float scan = sin(input.positionWS.y * _ScanlineFreq - _Time.y * _ScanlineSpeed);
                scan = scan * 0.5 + 0.5;
                scan = pow(scan, 2.2);

                // 3. Coordinate Grid (Latitude & Longitude coordinate mesh)
                float lat = pow(abs(sin(input.uv.y * 3.14159 * _GridLinesLat)), _GridSharpness);
                float lon = pow(abs(sin(input.uv.x * 6.28318 * _GridLinesLon)), _GridSharpness);
                float grid = saturate(lat + lon);

                // Equator & Tropics Accent Rings
                float equatorDist = abs(input.uv.y - 0.5);
                float equatorRing = pow(saturate(1.0 - equatorDist * 35.0), 3.0);
                float tropicNorth = pow(saturate(1.0 - abs(input.uv.y - 0.70) * 40.0), 3.0);
                float tropicSouth = pow(saturate(1.0 - abs(input.uv.y - 0.30) * 40.0), 3.0);
                float specialRings = equatorRing * 1.5 + (tropicNorth + tropicSouth) * 0.6;

                // 4. Procedural Continents & Coastlines
                float2 continentUv = float2(input.uv.x * 2.0, input.uv.y) * _ContinentScale;
                // Add slow planetary weather/crust rotation
                continentUv.x += _Time.y * 0.04;
                float land = fbm(continentUv);
                float isLand = smoothstep(0.48, 0.54, land);
                float coast = smoothstep(0.46, 0.49, land) - smoothstep(0.53, 0.56, land);
                coast = saturate(coast * 2.5);

                // 5. Vertical Radar Scanning Pulse (Pulse wave passing through the globe)
                float wave = frac(input.positionWS.y * 0.65 - _Time.y * _PulseSpeed);
                float pulseWave = smoothstep(0.0, 0.08, wave) * smoothstep(0.22, 0.08, wave);

                // 6. CRT Micro-Flicker
                float flicker = 0.92 + 0.08 * sin(_Time.y * _FlickerSpeed) * cos(_Time.y * (_FlickerSpeed * 0.63));

                // 7. Layer Composition
                // Backface is rendered slightly dimmer for deep internal volume look
                float faceDim = isFrontFace ? 1.0 : 0.45;

                // Base ocean body
                float3 col = _MainColor.rgb * (_InnerAlpha + scan * _ScanlineStrength);

                // Continents: Land glow + extra bright coastlines
                col += _GridColor.rgb * (isLand * 0.45 + coast * _CoastlineGlow);

                // Coordinate lines & Equator
                col += _GridColor.rgb * (grid * 1.25);
                col += _EquatorColor.rgb * specialRings;

                // Bright Fresnel rim on edges
                col += _RimColor.rgb * (fresnel * 2.0);

                // Vertical scan pulse
                col += _RimColor.rgb * (pulseWave * 1.4);

                col *= flicker * faceDim;

                // Transparency calculation
                float alpha = saturate(_InnerAlpha + fresnel * 0.85 + grid * 0.6 + isLand * 0.35 + coast * 0.5 + pulseWave * 0.4) * _Alpha;

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}

