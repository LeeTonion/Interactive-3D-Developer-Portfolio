Shader "SpaceCyberpunk/ModelScanningShader"
{
    Properties
    {
        [HDR] _ScanColor ("Scan Laser Color (HDR)", Color) = (0.0, 1.0, 1.0, 1.0)
        [HDR] _SecondaryColor ("Hologram Edge Color", Color) = (0.0, 0.75, 1.0, 1.0)
        _ScanProgress ("Scan Progress (0 to 1)", Float) = 0.0
        _BandWidth ("Laser Band Width", Float) = 0.08
        _TrailLength ("Trail Decay Length", Float) = 0.40
        _GridFreq ("3D Grid Frequency", Float) = 28.0
        _FresnelPower ("Fresnel Rim Power", Float) = 2.5
        _Alpha ("Master Alpha", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+150" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Pass
        {
            Name "ModelScanningPass"
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

            CBUFFER_START(UnityPerMaterial)
                float4 _ScanColor;
                float4 _SecondaryColor;
                float _ScanProgress;
                float _BandWidth;
                float _TrailLength;
                float _GridFreq;
                float _FresnelPower;
                float _Alpha;
            CBUFFER_END

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
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);
                float3 normalWS = normalize(input.normalWS);

                // Tọa độ Object Space Z chuẩn hóa: đuôi tàu = 0.0, mũi tàu = 1.0
                // craft_racer có min.z = -1.01, max.z = +1.01
                float normZ = (input.positionOS.z + 1.05) / 2.10;
                float delta = normZ - _ScanProgress;

                // 1. Đường Laser Quét Sắc Nét chạy dọc theo thân tàu 3D
                float leadingEdge = 1.0 - smoothstep(0.0, _BandWidth, abs(delta));
                leadingEdge = pow(saturate(leadingEdge), 1.8);

                // 2. Vệt phát quang Hologram Trail phía sau tia laser
                float trail = 0.0;
                if (delta <= 0.0 && delta >= -_TrailLength)
                {
                    float u = (-delta) / _TrailLength;
                    trail = (1.0 - u * u);
                }

                // Nếu không nằm trong phạm vi tia quét thì discard ngay để giữ nguyên 100% vỏ tàu gốc
                float scanInfluence = leadingEdge + trail * 0.75;
                if (scanInfluence <= 0.005)
                {
                    discard;
                }

                // 3. Lưới ma trận Hologram Grid tinh tế
                float gridX = sin(input.positionOS.x * _GridFreq);
                float gridY = sin(input.positionOS.y * _GridFreq);
                float grid = saturate(abs(gridX) * abs(gridY) * 2.5);

                // 4. Viền Fresnel Rim Glow tôn đường nét 3D
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);
                float rimGlow = fresnel * (leadingEdge * 2.2 + trail * 1.0);

                // 5. Màu phát quang Additive siêu thực
                float3 laserColor = _ScanColor.rgb * (leadingEdge * 4.0);
                float3 holoColor = _SecondaryColor.rgb * (grid * trail * 1.5 + rimGlow * 1.2);
                float3 finalColor = laserColor + holoColor;

                float totalAlpha = saturate(leadingEdge * 1.0 + trail * 0.55 + rimGlow * 0.6) * _Alpha;

                return half4(finalColor, totalAlpha);
            }
            ENDHLSL
        }
    }
}
