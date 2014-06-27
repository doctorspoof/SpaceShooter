Shader "Custom/Support Shield Pulse" {
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
				//wibble effects
				float4 _ImpactPositions[5] = {_ImpactPos1, _ImpactPos2, _ImpactPos3, _ImpactPos4, _ImpactPos5};
				float _ImpactTimes[5] = {_ImpactTime1, _ImpactTime2, _ImpactTime3, _ImpactTime4, _ImpactTime5};
				float effects[5] = {0, 0, 0, 0, 0};
				
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
				
				//Pulse effect
				float decimal = _Time.x - (int)_Time.x;
				decimal = decimal * 2.0f;
				decimal = decimal - (int)decimal;
				//decimal = decimal * 2.0f;
				//if(decimal > 1.0f)
					//decimal = decimal - 1.0f;
				
				float pulseDistance = 14.5f * decimal;
				float pulseMinDistance = 12.5f * decimal * 0.75f;
				float2 centre = float2(0,0);
				float distanceFromCentre = distance(centre, i.usePos.xy);
				float pulseEffect = 0;
				
				//Edge shimmer
				float edgeEffect = 0.0f;
				float edgeMultiplier = 1.0f;
				if(distanceFromCentre > 11.25f)
				{
					float edgeDistance = 1.0f;
					float midDistance = 0.5f * edgeDistance;
					
					float edgeCentreDistance = 11.25f + midDistance;
					float distanceToEdgeCentre = abs(edgeCentreDistance - distanceFromCentre);
					
					edgeEffect = 0.35f * (1.0f - (distanceToEdgeCentre * (1.0f / (edgeDistance * 0.5f))));
				}
				
				if(pulseDistance > 12.5f)
				{
					//0 @ 14.5f, 1 @ 13.5f
					float effect = clamp(abs(14.5f - pulseDistance), 0.0f, 1.0f);
					//edgeMultiplier = effect * 0.0833333333333333333333333f;
					edgeMultiplier = effect;
				}
				
				pulseDistance = clamp(pulseDistance, 0.0f, 13.25f);
				//Calculate pulse
				if(distanceFromCentre < pulseDistance && distanceFromCentre > pulseMinDistance)
				{
					float ringDistance = pulseDistance - pulseMinDistance;
					float midDistance = 0.5f * ringDistance;
					
					float ringCentreDistance = pulseMinDistance + midDistance;
					float distanceToRingCentre = abs(ringCentreDistance - distanceFromCentre);
					
					pulseEffect = 0.65f * (1.0f - (distanceToRingCentre * (1.0f / (ringDistance * 0.5f))));
				}
				
				
				
				float4 tex = tex2D(_MainTex, i.texcoord0.xy);
				
				//Alpha = (Base value + pulse + wibble effects) * texture's normal alpha
				tex.a = (0.15f + edgeEffect + (pulseEffect * edgeMultiplier) + (effects[0] + effects[1] + effects[2] + effects[3] + effects[4])) * tex.a;
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
