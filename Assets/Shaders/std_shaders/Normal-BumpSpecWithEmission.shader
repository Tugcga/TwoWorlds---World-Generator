// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Legacy Shaders/Bumped Specular with Emission" {
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
    [PowerSlider(5.0)] _Shininess ("Shininess", Range (0.03, 1)) = 0.078125
    _BumpMap ("Normalmap", 2D) = "bump" {}
    _EmissionMap("Emissionmap", 2D) = "emission" {}
    _EmissionColor("Emission Color", Color) = (1.0, 1.0, 1.0, 1)
    _Exposure("Exposure", Range(0.001, 10)) = 0.1
}

CGINCLUDE
sampler2D _BumpMap;
sampler2D _EmissionMap;
fixed4 _Color;
fixed4 _EmissionColor;
half _Shininess;
half _Exposure;

struct Input {
    float2 uv_BumpMap;
    float2 uv_EmissionMap;
};

void surf (Input IN, inout SurfaceOutput o) {
    fixed4 em = tex2D(_EmissionMap, IN.uv_EmissionMap);
    o.Albedo = _Color.rgb + em.rgb * _EmissionColor.rgb * _Exposure;
    o.Gloss = _SpecColor;
    o.Specular = _Shininess;
    o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
}
ENDCG

SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 400

    CGPROGRAM
    #pragma surface surf BlinnPhong
    #pragma target 3.0
    ENDCG
}

SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 400

    CGPROGRAM
    #pragma surface surf BlinnPhong nodynlightmap
    ENDCG
}

FallBack "Legacy Shaders/Specular"
}
