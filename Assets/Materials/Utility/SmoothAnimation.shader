Shader "Custom/SmoothAnimation" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
        _FrameNumX ("X Frames", int) = 1
        _FrameNumY ("Y Frames", int) = 1
        _FPS ("Frames Per Second", float) = 30.0
        _CurrentFrame ("Current Frame (DONT TOUCH)", float) = 0.0
        _StoredDeltaTime ("StoredDelta (DONT TOUCH)", float) = 0.0
        _TESTOUTPUT ("TEST", float) = 0.0
	}
	SubShader {
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		
        Pass {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            uniform sampler2D _MainTex;
            uniform int _FrameNumX;
            uniform int _FrameNumY;
            uniform float _FPS;
            uniform float _CurrentFrame;
            uniform float _StoredDeltaTime;
            uniform float _TESTOUTPUT;
            
            struct vertexInput {
                float4 vertex : POSITION;
                float4 texcoord0 : TEXCOORD0;
            };
            
            struct fragmentInput {
                float4 position : SV_POSITION;
                float4 texcoord0 : TEXCOORD0;
            };
            
            fragmentInput vert(vertexInput i)
            {
                fragmentInput o;
                o.position = mul(UNITY_MATRIX_MVP, i.vertex);
                o.texcoord0 = i.texcoord0;
                
                //_StoredDeltaTime = _StoredDeltaTime + unity_DeltaTime.x;
                return o;
            }
            
            float4 frag(fragmentInput i) : COLOR
            {
                //float localStoredDTime = _StoredDeltaTime + unity_DeltaTime.x;
                float inverseFPS = 1.0 / (float)_FPS;
                int totalFrames = _FrameNumX * _FrameNumY;
                
                float currFrameOverall = fmod(_StoredDeltaTime / inverseFPS, totalFrames); 
                //int currFrameOverall = _StoredDeltaTime / inverseFPS;
                
                //Now we know the frame, we can work out where the texcoord should be
                int currFrameX = fmod(currFrameOverall, (float)_FrameNumX);
                int currFrameY = currFrameOverall / (float)_FrameNumX;
                float catchFl = currFrameOverall - (currFrameX + (currFrameY * _FrameNumX));
                
                float percentFrameWidth = 1.0 / (float)_FrameNumX;
                float percentFrameHeight = 1.0 / (float)_FrameNumY;
                
                float2 offsetPos = float2(percentFrameWidth * currFrameX, 1.0 - (percentFrameHeight * currFrameY));
                float2 outputPos = float2((i.texcoord0.x / _FrameNumX) + offsetPos.x, (i.texcoord0.y / _FrameNumY) + offsetPos.y);
                
                //Update stored vars
                _CurrentFrame = currFrameOverall + catchFl;
                
                //Default
                float4 col = tex2D(_MainTex, outputPos.xy);
                //float4 col = float4();
                return col;
            }
            
            ENDCG
		}
	} 
	FallBack "Diffuse"
}
