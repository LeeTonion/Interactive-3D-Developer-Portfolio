Shader "SpaceCyberpunk/SciFiDeskScreenAnimated"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        
        [Header(Screen Identification)]
        [KeywordEnum(TelemetryRadar, DiagnosticsEqualizer)] _Mode ("Screen Mode", Float) = 0
        [HDR] _PrimaryColor ("Primary HUD Color (HDR)", Color) = (0.0, 0.9, 1.0, 1.0)
        [HDR] _SecondaryColor ("Secondary HUD Color (HDR)", Color) = (1.0, 0.5, 0.1, 1.0)
        _AnimSpeed ("Animation Speed", Float) = 1.0
        _GlowIntensity ("Emission Intensity", Float) = 2.5
        _ScanlineDensity ("Scanline Density", Float) = 100.0
        _NoiseFlicker ("CRT Flicker", Range(0, 1)) = 0.12
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "Queue" = "Geometry" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _MODE_TELEMETRYRADAR _MODE_DIAGNOSTICSEQUALIZER

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _PrimaryColor;
                float4 _SecondaryColor;
                float _AnimSpeed;
                float _GlowIntensity;
                float _ScanlineDensity;
                float _NoiseFlicker;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            // Pseudo-random helper
            float hash(float n)
            {
                return frac(sin(n) * 43758.5453123);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;

                // Screen coordinates in computer_wall_texture.png:
                // X in [68 .. 521] / 1024.0 -> [0.0664 .. 0.5088]
                // Y in [423 .. 738] / 1024.0 -> [0.4131 .. 0.7207]
                const float U_MIN = 0.0664;
                const float U_MAX = 0.5088;
                const float V_MIN = 0.4131;
                const float V_MAX = 0.7207;

                bool isScreen = (uv.x >= U_MIN && uv.x <= U_MAX && uv.y >= V_MIN && uv.y <= V_MAX);

                // If outside screen, calculate simple diffuse lighting and return baseTex
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalize(input.normalWS), mainLight.direction));
                half3 ambient = half3(0.18, 0.20, 0.24);
                half3 diffuse = baseTex.rgb * (ambient + mainLight.color * NdotL * 0.7);

                if (!isScreen)
                {
                    return half4(diffuse, baseTex.a);
                }

                // Normalized Screen Space UV [0, 1]
                float2 suv;
                suv.x = (uv.x - U_MIN) / (U_MAX - U_MIN);
                suv.y = (uv.y - V_MIN) / (V_MAX - V_MIN);

                // Spatial phase seed per desk workstation so surrounding desks aren't cloned identical
                float deskSeed = frac(sin(dot(floor(input.positionWS.xz * 0.4), float2(12.9898, 78.233))) * 43758.5453);
                float t = (_Time.y + deskSeed * 4.31) * _AnimSpeed;
                half3 hudColor = half3(0, 0, 0);

                // Global CRT Scanlines & Flicker
                float scanline = 0.82 + 0.18 * sin(suv.y * _ScanlineDensity - t * 6.0);
                float crtFlicker = 1.0 + _NoiseFlicker * (sin(t * 55.0) * 0.04 + cos(t * 110.0) * 0.03);

                #if defined(_MODE_DIAGNOSTICSEQUALIZER)
                // ═════════════════════════════════════════════════════════════════
                // MODE 1: DIAGNOSTICS & SPECTRUM EQUALIZER (Amber / Orange / Pink)
                // ═════════════════════════════════════════════════════════════════
                half3 colAmber = _PrimaryColor.rgb;
                half3 colPink  = _SecondaryColor.rgb;
                half3 colCyan  = half3(0.1, 0.85, 1.0);

                // A. Left Sub-Panel: Rotating Power Gyroscope / Circular Reactor
                float2 gyroCenter = float2(0.24, 0.50);
                float2 dG = (suv - gyroCenter) * float2(1.0, 0.70);
                float rG = length(dG);
                float angG = atan2(dG.y, dG.x);

                if (rG < 0.42)
                {
                    // Outer segmented ring (clockwise)
                    if (rG > 0.32 && rG < 0.38)
                    {
                        float notched = step(0.35, frac((angG + t * 1.5) * 2.546)); // 16 ticks
                        hudColor += colAmber * notched * 1.6;
                    }
                    // Middle segmented ring (counter-clockwise)
                    if (rG > 0.22 && rG < 0.27)
                    {
                        float notched = step(0.30, frac((-angG + t * 2.2) * 1.273)); // 8 ticks
                        hudColor += colPink * notched * 1.8;
                    }
                    // Inner glowing reactor core
                    if (rG < 0.16)
                    {
                        float pulse = 0.65 + 0.35 * sin(t * 5.0);
                        float coreGlow = smoothstep(0.16, 0.02, rG) * pulse;
                        hudColor += lerp(colAmber, colCyan, rG * 5.0) * coreGlow * 2.2;

                        // Center rotating cross
                        float crossRot = angG + t * 3.0;
                        if (abs(sin(crossRot * 2.0)) < 0.12 && rG > 0.05)
                        {
                            hudColor += colAmber * 2.0;
                        }
                    }
                    // Concentric thin guidelines
                    float ring1 = smoothstep(0.008, 0.0, abs(rG - 0.40));
                    float ring2 = smoothstep(0.006, 0.0, abs(rG - 0.19));
                    hudColor += colAmber * (ring1 * 0.7 + ring2 * 0.5);
                }

                // B. Center Divider Line
                float divLine = smoothstep(0.006, 0.0, abs(suv.x - 0.48));
                hudColor += colPink * divLine * 0.9;

                // C. Right Sub-Panel: 8-Channel Spectrum Equalizer
                if (suv.x >= 0.52 && suv.x <= 0.96 && suv.y >= 0.12 && suv.y <= 0.88)
                {
                    int ch = (int)clamp((suv.x - 0.52) / 0.055, 0.0, 7.0);
                    float colX = 0.52 + ch * 0.055 + 0.025;
                    float inCol = smoothstep(0.020, 0.016, abs(suv.x - colX));

                    if (inCol > 0.01)
                    {
                        // Dynamic jumping height per channel
                        float seed = (float)ch * 1.7;
                        float h = 0.20 + 0.60 * (0.5 + 0.5 * sin(t * (3.5 + (float)ch * 1.4) + seed) * cos(t * 1.8 + seed * 0.8));

                        // LED segmented gaps
                        float ledSegment = step(0.28, frac((suv.y - 0.12) * 16.0));

                        if (suv.y <= h)
                        {
                            // Height color gradient: Amber -> Orange -> Pink/Magenta
                            half3 barCol = lerp(colAmber, colPink, smoothstep(0.2, 0.75, suv.y));
                            hudColor += barCol * inCol * ledSegment * 2.0;
                        }

                        // Floating peak hold dot
                        float peakH = h + 0.03 * (0.5 + 0.5 * sin(t * 1.2 + seed));
                        float peakDot = smoothstep(0.015, 0.0, abs(suv.y - peakH));
                        hudColor += half3(1.0, 1.0, 1.0) * inCol * peakDot * 2.5;
                    }

                    // Background spectrum grid lines
                    float eqGrid = step(0.92, frac((suv.y - 0.12) * 8.0)) * 0.15;
                    hudColor += colAmber * eqGrid;
                }

                #else
                // ═════════════════════════════════════════════════════════════════
                // MODE 0: TELEMETRY & ROTATING RADAR (Cyan / Ice-Blue Theme)
                // ═════════════════════════════════════════════════════════════════
                half3 colCyan   = _PrimaryColor.rgb;
                half3 colGreen  = _SecondaryColor.rgb;
                half3 colWhite  = half3(0.9, 0.96, 1.0);

                // A. Left Sub-Panel: Rotating Radar Scope
                float2 radarCenter = float2(0.24, 0.50);
                float2 dR = (suv - radarCenter) * float2(1.0, 0.70);
                float rR = length(dR);
                float angR = atan2(dR.y, dR.x); // [-PI, PI]

                if (rR < 0.42)
                {
                    // Rotating radar beam (Clockwise)
                    float sweepAng = fmod(t * 2.6, 6.28318) - 3.14159;
                    float deltaAng = fmod(sweepAng - angR + 12.56637, 6.28318);

                    // Sharp sweep line
                    float beamLine = smoothstep(0.035, 0.0, deltaAng);
                    // Fading phosphor trail
                    float trail = exp(-deltaAng * 3.2);

                    hudColor += colCyan * (beamLine * 2.6 + trail * 0.95);

                    // Concentric radar grid rings
                    float ring1 = smoothstep(0.007, 0.0, abs(rR - 0.12));
                    float ring2 = smoothstep(0.007, 0.0, abs(rR - 0.24));
                    float ring3 = smoothstep(0.007, 0.0, abs(rR - 0.36));
                    float ringOuter = smoothstep(0.008, 0.0, abs(rR - 0.41));
                    hudColor += colCyan * (ring1 + ring2 + ring3) * 0.45;
                    hudColor += colWhite * ringOuter * 1.2;

                    // Axis crosshairs
                    float axisX = smoothstep(0.005, 0.0, abs(dR.y)) * step(rR, 0.41);
                    float axisY = smoothstep(0.005, 0.0, abs(dR.x)) * step(rR, 0.41);
                    hudColor += colCyan * (axisX + axisY) * 0.35;

                    // 4 Blinking Target Pings
                    float2 blip0 = float2(0.16, 0.65);
                    float2 blip1 = float2(0.34, 0.42);
                    float2 blip2 = float2(0.28, 0.68);
                    float2 blip3 = float2(0.15, 0.36);

                    float2 blips[4];
                    blips[0] = blip0; blips[1] = blip1; blips[2] = blip2; blips[3] = blip3;

                    for (int b = 0; b < 4; b++)
                    {
                        float2 bDir = (blips[b] - radarCenter) * float2(1.0, 0.70);
                        float bAng = atan2(bDir.y, bDir.x);
                        float bDelta = fmod(sweepAng - bAng + 12.56637, 6.28318);
                        float bIntensity = exp(-bDelta * 2.0); // Bright flash decaying as sweep moves away
                        float blipShape = smoothstep(0.016, 0.0, length(suv - blips[b]));
                        hudColor += (b % 2 == 0 ? colGreen : colWhite) * blipShape * bIntensity * 3.5;
                    }
                }

                // B. Center Divider Line
                float divLine = smoothstep(0.006, 0.0, abs(suv.x - 0.48));
                hudColor += colCyan * divLine * 0.9;

                // C. Right Sub-Panel Top: Real-Time Scrolling Matrix Data Stream (Y > 0.52)
                if (suv.x >= 0.51 && suv.x <= 0.96 && suv.y > 0.52 && suv.y < 0.92)
                {
                    float rowY = floor(suv.y * 14.0);
                    float colX = floor(suv.x * 24.0);
                    float charSeed = hash(rowY * 17.1 + colX * 31.7 + floor(t * 4.0));
                    float cellChar = step(0.35, charSeed);
                    float cellDim = 0.3 + 0.7 * hash(rowY + floor(t * 2.0));
                    hudColor += colCyan * cellChar * cellDim * 0.85;

                    // Horizontal divider bar
                    float hBar = smoothstep(0.006, 0.0, abs(suv.y - 0.52));
                    hudColor += colWhite * hBar * 1.2;
                }

                // D. Right Sub-Panel Bottom: Dynamic Oscilloscope / Telemetry Waveform (Y <= 0.52)
                if (suv.x >= 0.50 && suv.x <= 0.97 && suv.y >= 0.08 && suv.y <= 0.50)
                {
                    float relX = (suv.x - 0.50) / 0.47; // [0, 1]
                    // Primary ECG / sine pulse
                    float waveY = 0.28 + 0.12 * sin(relX * 32.0 - t * 8.0) * (0.7 + 0.3 * sin(t * 3.0 + relX * 5.0));
                    // Heartbeat spike modulation
                    float spikeDist = frac(relX * 2.0 - t * 0.7);
                    float spike = (smoothstep(0.1, 0.0, abs(spikeDist - 0.5)) - smoothstep(0.05, 0.0, abs(spikeDist - 0.53))) * 0.14;
                    waveY += spike;

                    float trace1 = smoothstep(0.012, 0.0, abs(suv.y - waveY));
                    float traceGlow = exp(-abs(suv.y - waveY) * 45.0);
                    hudColor += colGreen * trace1 * 2.5;
                    hudColor += colCyan * traceGlow * 1.2;

                    // Secondary harmonic wave
                    float waveY2 = 0.28 + 0.05 * cos(relX * 48.0 + t * 5.0);
                    float trace2 = smoothstep(0.008, 0.0, abs(suv.y - waveY2));
                    hudColor += colWhite * trace2 * 0.9;

                    // Subtle background waveform grid
                    float waveGrid = (step(0.94, frac(suv.x * 20.0)) + step(0.94, frac(suv.y * 18.0))) * 0.12;
                    hudColor += colCyan * waveGrid;
                }

                #endif

                // Apply Scanlines, CRT Flicker and Master Emission
                hudColor *= scanline * crtFlicker * _GlowIntensity;

                // Composite screen background (deep space navy / terminal black) + dynamic HUD
                half3 screenBG = half3(0.02, 0.04, 0.06);
                half3 finalScreen = screenBG + hudColor;

                return half4(finalScreen, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
