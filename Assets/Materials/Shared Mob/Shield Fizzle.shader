Shader "Custom/Shield Fizzle" {
	Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _ImpactPos1 ("Impact1", Vector) = (0, 0, 0, 0)
        _ImpactTime1 ("Time1", float) = 0.0
        _ImpactTypes1 ("Type1", int) = 0
        _ImpactMagnitude1 ("Magnitude1", float) = 0.0
        _ImpactPos2 ("Impact2", Vector) = (0, 0, 0, 0)
        _ImpactTime2 ("Time2", float) = 0.0
        _ImpactTypes2 ("Type2", int) = 0
        _ImpactMagnitude2 ("Magnitude2", float) = 0.0
        _ImpactPos3 ("Impact3", Vector) = (0, 0, 0, 0)
        _ImpactTime3 ("Time3", float) = 0.0
        _ImpactTypes3 ("Type3", int) = 0
        _ImpactMagnitude3 ("Magnitude3", float) = 0.0
        _ImpactPos4 ("Impact4", Vector) = (0, 0, 0, 0)
        _ImpactTime4 ("Time4", float) = 0.0
        _ImpactTypes4 ("Type4", int) = 0
        _ImpactMagnitude4 ("Magnitude4", float) = 0.0
        _ImpactPos5 ("Impact5", Vector) = (0, 0, 0, 0)
        _ImpactTime5 ("Time5", float) = 0.0
        _ImpactTypes5 ("Type5", int) = 0
        _ImpactMagnitude5 ("Magnitude5", float) = 0.0
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
            uniform int _ImpactTypes1;
            uniform float _ImpactMagnitude1;
            uniform float4 _ImpactPos2;
            uniform float _ImpactTime2;
            uniform int _ImpactTypes2;
            uniform float _ImpactMagnitude2;
            uniform float4 _ImpactPos3;
            uniform float _ImpactTime3;
            uniform int _ImpactTypes3;
            uniform float _ImpactMagnitude3;
            uniform float4 _ImpactPos4;
            uniform float _ImpactTime4;
            uniform int _ImpactTypes4;
            uniform float _ImpactMagnitude4;
            uniform float4 _ImpactPos5;
            uniform float _ImpactTime5;
            uniform int _ImpactTypes5;
            uniform float _ImpactMagnitude5;
            
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
				float _ImpactTimes[5] = {_ImpactTime1, _ImpactTime2, _ImpactTime3, _ImpactTime4, _ImpactTime5};
				int _ImpactTypes[5] = {_ImpactTypes1, _ImpactTypes2, _ImpactTypes3, _ImpactTypes4, _ImpactTypes5};
				float _ImpactMagnitudes[5] = {_ImpactMagnitude1, _ImpactMagnitude2, _ImpactMagnitude3, _ImpactMagnitude4, _ImpactMagnitude5};
			
				float effects[5] = {0, 0, 0, 0, 0};
				
				float staticExplodePulseDist = 3.0f;

				for(int j = 0; j < 5; j++)
				{
					if(_ImpactTypes[j] == 0)
					{
						//If physical, do the standard 'block'
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
					else if(_ImpactTypes[j] == 1)
					{
						//If energy, maybe do something different? For now just do default block
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
					else if(_ImpactTypes[j] == 2)
					{
						//Do laser fizzle - default to standard block for now
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
					else if(_ImpactTypes[j] == 3)
					{
						//Do explode pulse
						float4 imageSpPos = float4(_ImpactPositions[j].x + 0.5, _ImpactPositions[j].y + 0.5, _ImpactPositions[j].z + 0.5, _ImpactPositions[j].w);
						
						//Basic block stuff
						float distToImpact = distance(imageSpPos.xy, i.usePos.xy);
						float distDiff = 2.0 - distToImpact;
						float effect = 0;
						if(distDiff <= 0)
							effect = 0;
						else
							effect = distDiff / 2.0;
						
						//Shockwave stuff
						float currentPulseDistance = (1 - _ImpactTimes[j]) * (staticExplodePulseDist);
						
						float distBetweenImpactAndPulse = abs(currentPulseDistance - distToImpact);
						float shockEffect = 0.0f;
						if(distBetweenImpactAndPulse < 0.2f)
						{
							shockEffect = _ImpactTimes[j] * ((0.2f - distBetweenImpactAndPulse) / 0.2f) * (_ImpactMagnitudes[j] / 15.0f);
						}
						
						//Add them together
						effects[j] = shockEffect + (effect * _ImpactTimes[j]);
					}
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
