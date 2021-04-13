// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Legacy Shaders/Bumped Specular Diffuse with LightMap" {
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _SpecColor("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
    [PowerSlider(5.0)] _Shininess("Shininess", Range(0.03, 1)) = 0.078125
    //_MainTex ("Base (RGB)", 2D) = "white" {}
    _BumpMap ("Normalmap", 2D) = "bump" {}
    _LightMap ("Lightmap (RGB)", 2D) = "black" {}
    _Ambient("Ambient", Color) = (0, 0, 0, 1)
}

SubShader {
    LOD 300
    Tags { "RenderType" = "Opaque" }
CGPROGRAM
#pragma surface surf BlinnPhong nodynlightmap nolightmap noambient nodirlightmap 
struct Input {
  //float2 uv_MainTex;
  float2 uv_BumpMap;
  float2 uv2_LightMap;
};

//sampler2D _MainTex;
sampler2D _BumpMap;
sampler2D _LightMap;
fixed4 _Color;
half _Shininess;
fixed4 _Ambient;

void surf (Input IN, inout SurfaceOutput o)
{
    //fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
    o.Albedo = _Color.rgb;
  half4 lm = tex2D (_LightMap, IN.uv2_LightMap);
  o.Emission = lm.rgb*o.Albedo.rgb + _Ambient.rgb;
  o.Alpha = lm.a * _Color.a;
  o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
  //o.Gloss = tex.a;
  o.Gloss = _SpecColor;
  o.Specular = _Shininess;
}
ENDCG

}
FallBack "Legacy Shaders/Lightmapped/Diffuse"
}
