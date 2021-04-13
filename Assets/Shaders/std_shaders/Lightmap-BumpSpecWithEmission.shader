// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Legacy Shaders/Bumped Specular with Emission with Lightmap" {
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
    [PowerSlider(5.0)] _Shininess ("Shininess", Range (0.03, 1)) = 0.078125
    _BumpMap ("Normalmap", 2D) = "bump" {}
    _EmissionMap("Emissionmap", 2D) = "emission" {}
    _EmissionColor("Emission Color", Color) = (1.0, 1.0, 1.0, 1)
    _Exposure("Exposure", Range(0.001, 10)) = 0.1
    _LightMap("Lightmap (RGB)", 2D) = "black" {}
    _Ambient("Ambient", Color) = (0, 0, 0, 1)
}

CGINCLUDE
sampler2D _BumpMap;
sampler2D _EmissionMap;
sampler2D _LightMap;
fixed4 _Color;
fixed4 _EmissionColor;
half _Shininess;
half _Exposure;
fixed4 _Ambient;

struct Input {
    float2 uv_BumpMap;
    float2 uv_EmissionMap;
    float2 uv2_LightMap;
};

void surf (Input IN, inout SurfaceOutput o) {
    fixed4 em = tex2D(_EmissionMap, IN.uv_EmissionMap);
    half4 lm = tex2D(_LightMap, IN.uv2_LightMap);
    o.Albedo = _Color.rgb + em.rgb * _EmissionColor.rgb * _Exposure;
    o.Emission = lm.rgb * o.Albedo.rgb + _Ambient.rgb;
    o.Gloss = _SpecColor;
    o.Specular = _Shininess;
    o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
}
ENDCG

SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 400

    CGPROGRAM
    #pragma surface surf BlinnPhong nodynlightmap nolightmap noambient nodirlightmap 
    #pragma target 3.0
    ENDCG
}

SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 400

    CGPROGRAM
    #pragma surface surf BlinnPhong nodynlightmap nolightmap noambient nodirlightmap 
    ENDCG
}

FallBack "Legacy Shaders/Specular"
}
