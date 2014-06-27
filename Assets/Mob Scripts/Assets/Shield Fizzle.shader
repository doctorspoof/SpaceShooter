Shader "Custom/Shield Fizzle" {
	Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _ImpactPos1 ("Impact1", Vector) = (0, 0, 0, 0)
        _ImpactTime1 ("Time1", float) = 0.0
        _ImpactPos2 ("Impact2", Vector) = (0, 0, 0, 0)
        _ImpactTime2 ("Time2", float) = 0.0
        _ImpactPos3 ("Impact3", Vector) = (0, 0, 0, 0)
        _ImpactTime3 ("Time3", float) = 0.0
        _ImpactPos4 ("Impact4", Vector) = (0, 0, 0, 0)
        _ImpactTime4 ("Time4", float) = 0.0
        _ImpactPos5 ("Impact5", Vector) = (0, 0, 0, 0)
        _ImpactTime5 ("Time5", float) = 0.0
    }
    SubShader {
    Tags{"Queue"="Transparent" "RenderType"="Transparent"}
    
        Pass {
        	ZWrite Off
        	Blend SrcAlpha OneMinusSrcAlpha
        	
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            uniform sampler2D _MainTex;
            uniform float4 _ImpactPos1;
            uniform float _ImpactTime1;
            uniform float4 _ImpactPos2;
            uniform float _ImpactTime2;
            uniform float4 _ImpactPos3;
            uniform float _ImpactTime3;
            uniform float4 _ImpactPos4;
            uniform float _ImpactTime4;
            uniform float4 _ImpactPos5;
            uniform float _ImpactTime5;
            
            struct vertexInput {
            	float4 vertex : POSITION;
            	float4 texcoord0 : TEXCOORD0;
    		};
    		
    		struct fragmentInput{
    			float4 position : SV_POSITION;
    			float4 texcoord0 : TEXCOORD0;
    			float4 usePos : TEXCOORD1;
    		};

			fragmentInput vert(vertexInput i)
			{
				fragmentInput o;
				o.position = mul(UNITY_MATRIX_MVP, i.vertex);
				o.texcoord0 = i.texcoord0;
				o.usePos = i.vertex;
				return o;
			}

			float4 frag(fragmentInput i) : COLOR 
			{
				float4 _ImpactPositions[5] = {_ImpactPos1, _ImpactPos2, _ImpactPos3, _ImpactPos4, _ImpactPos5};
				//float4 _ImpactPositions[5];
				//_ImpactPositions[0] = _ImpactPos1;
				//_ImpactPositions[1] = _ImpactPos2;
				//_ImpactPositions[2] = _ImpactPos3;
				//_ImpactPositions[3] = _ImpactPos4;
				//_ImpactPositions[4] = _ImpactPos5;
				
				float _ImpactTimes[5] = {_ImpactTime1, _ImpactTime2, _ImpactTime3, _ImpactTime4, _ImpactTime5};
				//float _ImpactTimes[5];
				//_ImpactTimes[0] = _ImpactTime1;
				//_ImpactTimes[1] = _ImpactTime2;
				//_ImpactTimes[2] = _ImpactTime3;
				//_ImpactTimes[3] = _ImpactTime4;
				//_ImpactTimes[4] = _ImpactTime5;
			
				float effects[5] = {0, 0, 0, 0, 0};
				//float effects[5];
				//effects[0] = 0;
				//effects[1] = 0;
				//effects[2] = 0;
				//effects[3] = 0;
				//effects[4] = 0;
				
				for(int j = 0; j < 5; j++)
				{
					float4 imageSpPos = float4(_ImpactPositions[j].x + 0.5, _ImpactPositions[j].y + 0.5, _ImpactPositions[j].z + 0.5, _ImpactPositions[j].w);
					
					float distToImpact = distance(imageSpPos.xy, i.usePos.xy);
					float distDiff = 2.0 - distToImpact;
					float effect = 0;
					if(distDiff <= 0)
						effect = 0;
					else
						effect = distDiff / 2.0;
						
					effects[j] = effect * _ImpactTimes[j];
				}
							
				//Take localPosition from CShip, change to imageSpace
				//float4 imageSpPos = float4(_ImpactPos.x + 0.5, _ImpactPos.y + 0.5, _ImpactPos.z + 0.5, _ImpactPos.w);
			
				//Find distance between impactpos and this vertex/fragment
				//float distToImpact = distance(imageSpPos.xy, i.usePos.xy);
				//float distDiff = 2.0 - distToImpact;
				
				//float effect = 0;
				//if(distDiff <= 0)
				//	effect = 0;
				//else
				//	effect = distDiff / 2.0;
					
				//float finalAlpha = effect * _ImpactTime;*/
			
				
				float4 tex = tex2D(_MainTex, i.texcoord0.xy);
				
				tex.a = (effects[0] + effects[1] + effects[2] + effects[3] + effects[4]) * tex.a;
				//tex.a = finalAlpha * tex.a;
				return tex;
			}

            //float4 frag(v2f_img i) : COLOR 
            //{
                //float4 tex = tex2D(_MainTex, i.uv);
                //return tex;
            //}
            ENDCG
        }
    }
	FallBack "Diffuse"
}
