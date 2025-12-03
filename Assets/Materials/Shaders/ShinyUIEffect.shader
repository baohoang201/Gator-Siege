Shader "Unlit/ShinyUIEffect"
{
    Properties
    {
        _Rotation("Rotation", Float) = 4.25
        _CycleTime("CycleTime", Float) = 6
        _WaveSpeed("WaveSpeed", Float) = 3
        [NoScaleOffset]_MainTex("MainTex", 2D) = "white" {}
        _Reverse_Thickness("Reverse Thickness", Float) = 3
        _Thickness_Alpha("Thickness Alpha", Float) = 0.5
        _Shiny_Color("Shiny Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        LOD 100

        Pass
        {
            Name "Pass"
            
            // Render State
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            ZTest LEqual
            ZWrite Off
            ColorMask RGB

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            // Includes cơ bản của Unity
            #include "UnityCG.cginc"

            // Properties
            float _Rotation;
            float _CycleTime;
            float _WaveSpeed;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Reverse_Thickness;
            float _Thickness_Alpha;
            float4 _Shiny_Color;

            // Vertex input
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // Vertex output / Fragment input
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Hàm xoay UV (thay thế Unity_Rotate_Radians_float)
            float2 RotateUV(float2 uv, float2 center, float rotation)
            {
                uv -= center;
                float s = sin(rotation);
                float c = cos(rotation);
                float2x2 rMatrix = float2x2(c, -s, s, c);
                uv = mul(rMatrix, uv);
                uv += center;
                return uv;
            }

            // Hàm sóng tam giác (thay thế TriangleWave_float)
            float TriangleWave(float x)
            {
                return 2.0 * abs(2.0 * (x - floor(0.5 + x))) - 1.0;
            }

            // Vertex shader
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            // Fragment shader
            fixed4 frag(v2f i) : SV_Target
            {
                // Xoay UV
                float2 rotatedUV = RotateUV(i.uv, float2(0.5, 0.5), _Rotation);

                // Lấy kênh R của UV đã xoay và xử lý
                float uvR = rotatedUV.x;
                float offset = uvR + (-1.2);
                float thickness = offset * _Reverse_Thickness;

                // Tính thời gian tuần hoàn
                float timeMod = fmod(_Time.y, _CycleTime);
                float wave = timeMod * _WaveSpeed;

                // Kết hợp và xử lý sóng
                float combined = thickness + wave;
                float saturated = clamp(combined, 0.0, 1.0);
                float triWave = TriangleWave(saturated);
                float alpha = triWave * _Thickness_Alpha;
                float finalAlpha = clamp(alpha, 0.0, 1.0);

                // Tính màu sáng
                float4 shiny = _Shiny_Color * finalAlpha;

                // Lấy mẫu texture
                float4 texColor = tex2D(_MainTex, i.uv);

                // Kết hợp màu sáng và texture
                float4 finalColor = shiny + texColor;

                // Áp dụng fog
                UNITY_APPLY_FOG(i.fogCoord, finalColor);

                // Gán alpha từ texture
                finalColor.a = texColor.a;

                return finalColor;
            }
            ENDHLSL
        }
    }
}