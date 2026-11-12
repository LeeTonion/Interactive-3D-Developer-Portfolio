Shader "SpaceCyberpunk/SciFiBigScreenAnimated"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)

        [Header(Screen Animation Mode)]
        [KeywordEnum(RadarScope, CentralHolo, TelemetryBars, HoloHud)] _Mode ("Animation Mode", Float) = 0
        
        [Header(Visual Customization)]
        [HDR] _PrimaryColor ("Primary HUD Color (HDR)", Color) = (0.0, 1.0, 0.85, 1.0)
        [HDR] _SecondaryColor ("Secondary HUD Color (HDR)", Color) = (1.0, 0.45, 0.15, 1.0)
        _AnimSpeed ("Animation Speed", Float) = 1.0
        _GlowIntensity ("Emission Intensity", Float) = 2.5
        _ScanlineDensity ("Scanline Density", Float) = 140.0
        _NoiseFlicker ("CRT Flicker", Range(0, 1)) = 0.08
        
        [Header(Screen Geometry and Mapping)]
        _ScreenVMax ("Screen V Max (0.634 for modular pack screens)", Float) = 0.634
        _FlipV ("Flip V (1 for console_celing, 0 for big_screen)", Float) = 0.0
        _AspectCorrection ("Aspect Ratio (Width / Height)", Float) = 1.25
        _UseBaseTex ("Blend Base Texture (1=Yes, 0=Procedural Only)", Float) = 1.0
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
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _MODE_RADARSCOPE _MODE_CENTRALHOLO _MODE_TELEMETRYBARS _MODE_HOLOHUD

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
                float2 uv         : TEXCOORD1;
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
                float _ScreenVMax;
                float _FlipV;
                float _AspectCorrection;
                float _UseBaseTex;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            float hash(float n)
            {
                return frac(sin(n) * 43758.5453123);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // Normalize UV into [0, 1] x [0, 1] screen area
                float vMax = max(_ScreenVMax, 0.001);
                float vNorm = saturate(uv.y / vMax);
                if (_FlipV > 0.5)
                {
                    vNorm = 1.0 - vNorm;
                }

                float2 suv = float2(saturate(uv.x), vNorm);

                // Sample base texture remapped to full screen height so it is never chopped
                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, suv) * _BaseColor;
                if (_UseBaseTex < 0.5)
                {
                    baseTex = half4(0.01, 0.02, 0.03, 1.0);
                }

                float t = _Time.y * _AnimSpeed;

                // CRT Scanlines & Flicker
                float scanline = 0.88 + 0.12 * sin(suv.y * _ScanlineDensity - t * 5.0);
                float crtFlicker = 1.0 + _NoiseFlicker * (sin(t * 50.0) * 0.04 + cos(t * 110.0) * 0.03);

                half3 finalColor = baseTex.rgb;

                #if defined(_MODE_RADARSCOPE)
                // ═════════════════════════════════════════════════════════════════
                // MODE 0: COMPLETE FULL-CIRCLE TACTICAL RADAR SCOPE
                // Perfectly centered, zero clipping, complete 360-degree sweep
                // ═════════════════════════════════════════════════════════════════
                float AR = max(_AspectCorrection, 0.5);
                float2 center = float2(0.5, 0.5);
                float2 dR = float2((suv.x - center.x) * AR, suv.y - center.y);
                float rR = length(dR);
                float angR = atan2(dR.y, dR.x); // [-PI, PI]

                const float R_OUTER = 0.44; // Fits 100% within height 1.0 (dia = 0.88)

                half3 radarColor = half3(0.01, 0.03, 0.02);

                if (rR <= R_OUTER + 0.02)
                {
                    // Clockwise 360-degree Rotating Radar Sweep
                    float sweepAng = fmod(-t * 2.5, 6.28318) - 3.14159;
                    float deltaAng = fmod(sweepAng - angR + 12.56637, 6.28318);

                    // Sharp laser sweep line
                    float beamLine = smoothstep(0.035, 0.0, deltaAng) * step(rR, R_OUTER);
                    // Phosphor decay trail
                    float trail = exp(-deltaAng * 3.2) * step(rR, R_OUTER);
                    half3 sweepGlow = _PrimaryColor.rgb * (beamLine * 2.8 + trail * 1.0);

                    // 4 Concentric Range Rings (10km, 20km, 30km, 40km)
                    float ring1 = smoothstep(0.007, 0.0, abs(rR - 0.11));
                    float ring2 = smoothstep(0.007, 0.0, abs(rR - 0.22));
                    float ring3 = smoothstep(0.007, 0.0, abs(rR - 0.33));
                    float ring4 = smoothstep(0.008, 0.0, abs(rR - R_OUTER));
                    float ringPulse = 0.85 + 0.15 * sin(t * 3.0);
                    half3 rings = _PrimaryColor.rgb * (ring1 * 0.4 + ring2 * 0.4 + ring3 * 0.5 + ring4 * 1.4) * ringPulse;

                    // Cardinal Axes Crosshairs
                    float crossX = smoothstep(0.005, 0.0, abs(dR.y)) * step(rR, R_OUTER);
                    float crossY = smoothstep(0.005, 0.0, abs(dR.x)) * step(rR, R_OUTER);
                    half3 crosshair = _PrimaryColor.rgb * (crossX + crossY) * 0.45;

                    // Outer Compass Degree Ticks (Every 30 degrees)
                    float ticks = step(0.65, frac((angR + 3.14159) * 1.9098)) * smoothstep(R_OUTER - 0.03, R_OUTER, rR) * step(rR, R_OUTER);
                    half3 tickGlow = half3(1.0, 1.0, 1.0) * ticks * 1.5;

                    // 5 Dynamic Target Blips with Sonar Echo Rings
                    float2 blipOffsets[5];
                    blipOffsets[0] = float2( 0.18,  0.22);
                    blipOffsets[1] = float2(-0.25, -0.15);
                    blipOffsets[2] = float2( 0.28, -0.18);
                    blipOffsets[3] = float2(-0.16,  0.26);
                    blipOffsets[4] = float2( 0.06, -0.32);

                    half3 blipTotal = half3(0, 0, 0);
                    for (int b = 0; b < 5; b++)
                    {
                        float2 bDir = blipOffsets[b];
                        float bAng = atan2(bDir.y, bDir.x);
                        float bDelta = fmod(sweepAng - bAng + 12.56637, 6.28318);
                        float bFlash = exp(-bDelta * 2.5); // Flash on sweep

                        float2 blipScreenPos = center + float2(bDir.x / AR, bDir.y);
                        float dist = length(float2((suv.x - blipScreenPos.x) * AR, suv.y - blipScreenPos.y));
                        float blipDot = smoothstep(0.016, 0.0, dist) * bFlash * 3.8;

                        // Expanding echo ring
                        float echoR = bDelta * 0.04;
                        float echoRing = smoothstep(0.005, 0.0, abs(dist - echoR)) * bFlash * 1.2 * step(bDelta, 1.2);

                        blipTotal += (b % 2 == 0 ? _PrimaryColor.rgb : half3(1.0, 1.0, 0.8)) * (blipDot + echoRing);
                    }

                    radarColor = rings + crosshair + tickGlow + sweepGlow + blipTotal;
                }

                // Widescreen Flanking Tactical Telemetry Columns (Left & Right margins)
                half3 flankColor = half3(0, 0, 0);
                float xMargin = (suv.x - 0.5) * AR;

                // Left Flank (Status Columns & Altitude Bars)
                if (xMargin < -R_OUTER - 0.04 && suv.x >= 0.03)
                {
                    float rowY = floor(suv.y * 12.0);
                    float colX = floor((suv.x - 0.03) * 20.0);
                    float chLevel = 0.3 + 0.6 * (0.5 + 0.5 * sin(t * 3.0 + rowY * 1.2));
                    float inBar = step((suv.x - 0.03) / ((-R_OUTER / AR + 0.5) - 0.03), chLevel);
                    float seg = step(0.25, frac(suv.y * 24.0));
                    flankColor += _PrimaryColor.rgb * inBar * seg * 1.4;

                    // Digital text ticker scan
                    float textSeed = step(0.35, hash(rowY * 11.3 + colX * 19.7 + floor(t * 3.0)));
                    flankColor += half3(0.8, 1.0, 0.9) * textSeed * 0.6;
                }

                // Right Flank (Threat Histogram & Spectrum)
                if (xMargin > R_OUTER + 0.04 && suv.x <= 0.97)
                {
                    float rightRel = (suv.x - (0.5 + R_OUTER / AR + 0.04)) / (0.97 - (0.5 + R_OUTER / AR + 0.04));
                    float colIdx = floor(rightRel * 6.0);
                    float hTarget = 0.25 + 0.65 * (0.5 + 0.5 * sin(t * (3.5 + colIdx * 1.5) + colIdx * 2.1));
                    float inCol = step(suv.y, hTarget) * step(0.25, frac(rightRel * 6.0));
                    half3 colGrad = lerp(_PrimaryColor.rgb, _SecondaryColor.rgb, suv.y);
                    flankColor += colGrad * inCol * 1.8;

                    // Blinking status LED
                    float ledFlash = step(0.85, hash(colIdx * 17.1 + floor(t * 4.0)));
                    flankColor += _SecondaryColor.rgb * ledFlash * 1.5;
                }

                // Outer Frame Border
                float frameBorder = smoothstep(0.015, 0.0, min(min(suv.x - 0.01, 0.99 - suv.x), min(suv.y - 0.02, 0.98 - suv.y)));
                half3 frameGlow = _PrimaryColor.rgb * frameBorder * 1.5;

                finalColor = radarColor + flankColor + frameGlow;

                #elif defined(_MODE_CENTRALHOLO)
                // ═════════════════════════════════════════════════════════════════
                // MODE 1: MASTER CENTRAL HOLOGRAPHIC RETICLE & APERTURE
                // ═════════════════════════════════════════════════════════════════
                float AR_H = max(_AspectCorrection, 0.5);
                float2 dH = float2((suv.x - 0.5) * AR_H, suv.y - 0.5);
                float rH = length(dH);
                float angH = atan2(dH.y, dH.x);

                const float R_HOLO_OUTER = 0.44;

                half3 holoColor = baseTex.rgb * 0.3;

                if (rH <= R_HOLO_OUTER + 0.02)
                {
                    // A. Outer Segmented Ring Rotating Clockwise (r in [0.37 .. 0.43])
                    if (rH > 0.36 && rH < 0.44)
                    {
                        float ticks = step(0.35, frac((angH + t * 1.2) * 3.819)); // 24 ticks
                        holoColor += _PrimaryColor.rgb * ticks * 1.8;
                    }

                    // B. Middle Concentric Ring
                    float midRing = smoothstep(0.007, 0.0, abs(rH - 0.30));
                    holoColor += _PrimaryColor.rgb * midRing * 1.2;

                    // C. Inner Gear Ring Rotating Counter-Clockwise (r in [0.20 .. 0.26])
                    if (rH > 0.20 && rH < 0.26)
                    {
                        float gear = step(0.32, frac((-angH + t * 1.8) * 2.546)); // 16 ticks
                        holoColor += _SecondaryColor.rgb * gear * 2.0;
                    }

                    // D. Central Pulsating Aperture Core (r < 0.16)
                    if (rH < 0.16)
                    {
                        float corePulse = 0.70 + 0.30 * sin(t * 4.5);
                        float coreGlow = smoothstep(0.16, 0.01, rH) * corePulse;
                        holoColor += lerp(_PrimaryColor.rgb, half3(1.0, 1.0, 1.0), 0.5) * coreGlow * 2.8;

                        // Rotating crosshairs in core
                        float crossRot = angH + t * 2.2;
                        if (abs(sin(crossRot * 2.0)) < 0.08 && rH > 0.04)
                        {
                            holoColor += half3(1.0, 1.0, 1.0) * 2.0;
                        }
                    }

                    // E. Vertical Hologram Laser Scanning Beam
                    float scanY = 0.5 + 0.38 * sin(t * 1.8);
                    float laserBeam = smoothstep(0.012, 0.0, abs(suv.y - scanY)) * step(rH, R_HOLO_OUTER);
                    holoColor += _PrimaryColor.rgb * laserBeam * 2.2;
                }

                finalColor = holoColor;

                #elif defined(_MODE_TELEMETRYBARS)
                // ═════════════════════════════════════════════════════════════════
                // MODE 2: MULTI-CHANNEL SPECTRUM & TELEMETRY EQUALIZER BLOCKS
                // Full-screen dynamic telemetry, jumping levels, status LEDs
                // ═════════════════════════════════════════════════════════════════
                half3 blockAnimColor = half3(0.01, 0.02, 0.04);

                // Multi-row channel layout (10 rows, 16 columns)
                float rowY = floor(suv.y * 10.0);
                float colX = floor(suv.x * 16.0);

                // Dynamic channel jumping level per row
                float rowSeed = rowY * 7.19;
                float channelLevel = 0.30 + 0.65 * (0.5 + 0.5 * sin(t * (2.8 + rowY * 0.6) + rowSeed) * cos(t * 1.4 + rowSeed * 0.6));

                // Active block vs Standby block modulation
                float inBar = step(suv.x, channelLevel);
                float segX = step(0.18, frac(suv.x * 16.0));
                float segY = step(0.25, frac(suv.y * 10.0));
                float isBlock = segX * segY;

                // Color palette per row (Cyan, Green, Amber, Pink)
                half3 rowCol = _PrimaryColor.rgb;
                if (rowY < 3.0) rowCol = _PrimaryColor.rgb;
                else if (rowY < 6.0) rowCol = half3(0.2, 1.0, 0.5); // Bright Green
                else if (rowY < 8.0) rowCol = _SecondaryColor.rgb;   // Amber
                else rowCol = half3(1.0, 0.3, 0.7);                 // Magenta

                if (inBar > 0.5)
                {
                    float activePulse = 1.1 + 0.4 * sin(t * 6.0 + colX);
                    blockAnimColor = rowCol * activePulse * isBlock * 2.0;

                    // Edge peak indicator flash
                    if (abs(suv.x - channelLevel) < 0.05)
                    {
                        blockAnimColor += half3(1.0, 1.0, 1.0) * isBlock * 2.2;
                    }
                }
                else
                {
                    // Standby dim block
                    blockAnimColor = rowCol * 0.18 * isBlock;
                }

                // Random blinking packet transmission flashes
                float packetFlash = step(0.88, hash(rowY * 19.3 + colX * 31.7 + floor(t * 4.0)));
                blockAnimColor += half3(1.0, 1.0, 1.0) * packetFlash * isBlock * 1.5;

                // Horizontal data refresh laser sweep
                float waveX = frac(t * 0.35);
                float sweepLine = smoothstep(0.015, 0.0, abs(suv.x - waveX));
                blockAnimColor += _PrimaryColor.rgb * sweepLine * 1.6;

                // Lower section Oscilloscope wave (rowY == 0 or 1)
                if (suv.y < 0.18 && suv.y > 0.03)
                {
                    float waveY = 0.10 + 0.05 * sin(suv.x * 24.0 - t * 8.0) * (0.6 + 0.4 * sin(t * 3.0));
                    float waveLine = smoothstep(0.012, 0.0, abs(suv.y - waveY));
                    blockAnimColor += half3(1.0, 1.0, 1.0) * waveLine * 2.5;
                }

                finalColor = blockAnimColor;

                #elif defined(_MODE_HOLOHUD)
                // ═════════════════════════════════════════════════════════════════
                // MODE 3: SUSPENDED TACTICAL HOLO BANNER & DATA MATRIX STREAM
                // ═════════════════════════════════════════════════════════════════
                half3 hudOut = baseTex.rgb * 0.4;

                // A. Real-Time Scrolling Matrix Code Stream (Top Half)
                if (suv.y > 0.38)
                {
                    float row = floor((suv.y + t * 0.25) * 16.0);
                    float col = floor(suv.x * 28.0);
                    float charSeed = hash(row * 13.7 + col * 29.3 + floor(t * 3.0));
                    float charPixel = step(0.42, charSeed);
                    float charFade = 0.3 + 0.7 * hash(row + floor(t * 1.5));
                    hudOut += _PrimaryColor.rgb * charPixel * charFade * 1.2;
                }

                // B. Dynamic Signal Waveform / Oscilloscope (Bottom Half)
                if (suv.y <= 0.38 && suv.y >= 0.06)
                {
                    float waveY = 0.20 + 0.09 * sin(suv.x * 25.0 - t * 7.0) * (0.6 + 0.4 * sin(t * 2.0));
                    float waveLine = smoothstep(0.015, 0.0, abs(suv.y - waveY));
                    hudOut += _SecondaryColor.rgb * waveLine * 2.8;
                }

                // C. Sweeping Laser Scan Line
                float scanBar = smoothstep(0.012, 0.0, abs(suv.y - frac(t * 0.5)));
                hudOut += half3(1.0, 1.0, 1.0) * scanBar * 1.8;

                // D. Pulsating Outer Cybernetic Border Glow
                float borderDist = min(min(suv.x, 1.0 - suv.x), min(suv.y, 1.0 - suv.y));
                float borderGlow = smoothstep(0.035, 0.0, borderDist) * (0.7 + 0.3 * sin(t * 4.0));
                hudOut += _PrimaryColor.rgb * borderGlow * 2.2;

                finalColor = hudOut;
                #endif

                // Apply Global CRT Scanlines, Flicker & Master Emission Intensity
                finalColor *= scanline * crtFlicker * _GlowIntensity;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
