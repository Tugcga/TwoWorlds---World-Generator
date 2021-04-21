// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Legacy Shaders/Diffuse with LightMap" {
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _MainTex ("Base (RGB)", 2D) = "white" {}
    _LightMap ("Lightmap (RGB)", 2D) = "black" {}
    //_Ambient ("Ambient", Color) = (0, 0, 0, 1)
    //_Coeff ("Coefficient", Range(0, 4)) = 1
    //_Subtractor("Subtractor", Range(0, 1)) = 0
    _Multiplier("Multiplier", Range(0, 10)) = 1
    //_AmbientCoeff ("Ambient Coefficient", Range(0, 1)) = 1
}

SubShader {
    LOD 200
    Tags { "RenderType" = "Opaque" }
CGPROGRAM
#pragma surface surf Lambert nodynlightmap nolightmap noambient nodirlightmap 
struct Input {
  float2 uv_MainTex;
  float2 uv2_LightMap;
};
sampler2D _MainTex;
sampler2D _LightMap;
fixed4 _Color;
//fixed4 _Ambient;
//half _Coeff;
//half _AmbientCoeff;
//half _Subtractor;
half _Multiplier;
void surf (Input IN, inout SurfaceOutput o)
{
  o.Albedo = tex2D (_MainTex, IN.uv_MainTex).rgb * _Color;
  half4 lm = tex2D (_LightMap, IN.uv2_LightMap);
  //o.Emission = lm.rgb *o.Albedo.rgb + _Ambient.rgb;
  //o.Emission = (lm.rgb * _Coeff * tex2D(_MainTex, IN.uv_MainTex).rgb + _Ambient.rgb * _AmbientCoeff - _Subtractor) * _Multiplier;
  //o.Emission = lm.rgb * tex2D(_MainTex, IN.uv_MainTex).rgb * _Coeff;
  o.Emission = DecodeLightmap(lm) * o.Albedo * _Multiplier;
  //o.Albedo = o.Albedo;// + _Ambient.rgb * _AmbientCoeff;
  //o.Alpha = lm.a * _Color.a;
}
ENDCG
}
FallBack "Legacy Shaders/Lightmapped/VertexLit"
}
