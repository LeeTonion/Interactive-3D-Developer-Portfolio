Shader "SpaceCyberpunk/HologramBeamShader"
{
    Properties
    {
        [HDR] _BeamColor ("Beam Laser Color (HDR)", Color) = (0.15, 0.9, 1.0, 1.5)
        _ScanSpeed ("Ray Scroll Speed", Float) = 2.0
        _RayCount ("Vertical Light Rays", Float) = 24.0
        _PulseSpeed ("Upward Pulse Speed", Float) = 1.8
        _Alpha ("Master Beam Alpha", Range(0, 1)) = 0.45
    }

    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent+240" 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Pass
        {
            Name "HologramBeamForward"
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
                float4 _BeamColor;
                float _ScanSpeed;
                float _RayCount;
                float _PulseSpeed;
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
                // 1. Vertical rays around the cone
                float angle = atan2(input.positionOS.x, input.positionOS.z);
                float rays = sin(angle * _RayCount + _Time.y * _ScanSpeed);
                rays = pow(rays * 0.5 + 0.5, 3.0);

                // 2. Upward light energy pulses
                float pulse = frac(-input.positionOS.y * 1.5 + _Time.y * _PulseSpeed);
                float pulseGlow = smoothstep(0.0, 0.1, pulse) * smoothstep(0.4, 0.1, pulse);

                // 3. Vertical fade: maximum at base (near lens), soft decay near planet
                float vFade = saturate(1.0 - input.uv.y * 0.85);

                // 4. Fresnel edge glow
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);
                float3 normalWS = normalize(input.normalWS);
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), 1.8);

                float intensity = (rays * 0.7 + pulseGlow * 0.9 + fresnel * 0.6) * vFade;
                float3 col = _BeamColor.rgb * intensity * 1.8;
                float alpha = saturate(intensity * _Alpha);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}

