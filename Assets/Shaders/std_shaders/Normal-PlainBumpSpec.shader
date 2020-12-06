// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Legacy Shaders/Plain Bump Specular" {
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
    [PowerSlider(5.0)] _Shininess ("Shininess", Range (0.03, 1)) = 0.078125
    _BumpMap ("Normalmap", 2D) = "bump" {}
}

CGINCLUDE
sampler2D _BumpMap;
fixed4 _Color;
half _Shininess;

struct Input {
    float2 uv_BumpMap;
};

void surf (Input IN, inout SurfaceOutput o) {
    o.Albedo = _Color.rgb;
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
