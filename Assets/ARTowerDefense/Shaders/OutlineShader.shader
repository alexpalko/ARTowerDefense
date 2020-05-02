Shader "Outlined/Silhouetted Bumped Mobile" {
    Properties{
        _Color("Main Color", Color) = (.5,.5,.5,1)
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _Outline("Outline width", Float) = 4
        _MainTex("Base (RGB)", 2D) = "white" { }
        _BumpMap("Bumpmap", 2D) = "bump" {}
    }

        CGINCLUDE
#include "UnityCG.cginc"

        struct appdata {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
    };

    struct v2f {
        float4 pos : POSITION;
        float4 color : COLOR;
    };

    uniform float _Outline;
    uniform float4 _OutlineColor;

    v2f vert(appdata v) {
        
        v2f o;
        o.pos = v.vertex;
        o.pos.xyz += v.normal.xyz * _Outline * 0.01;
        o.pos = UnityObjectToClipPos(o.pos);

        o.color = _OutlineColor;
        return o;
    }
    ENDCG

        SubShader{
            Tags { "Queue" = "Transparent" }

            Pass {
                Name "OUTLINE"
                Tags { "LightMode" = "Always" }
                Cull Off
                ZWrite Off

        Blend SrcAlpha OneMinusSrcAlpha 

CGPROGRAM
#pragma vertex vert
#pragma fragment frag

half4 frag(v2f i) : COLOR {
    return i.color;
}
ENDCG
        }


CGPROGRAM
#pragma surface surf Lambert
struct Input {
    float2 uv_MainTex;
    float2 uv_BumpMap;
};
sampler2D _MainTex;
sampler2D _BumpMap;
uniform float3 _Color;
void surf(Input IN, inout SurfaceOutput o) {
    o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb * _Color;
    o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
}
ENDCG

    }

        SubShader{
            Tags { "Queue" = "Transparent" }

            Pass {
                Name "OUTLINE"
                Tags { "LightMode" = "Always" }
                Cull Front
                ZWrite Off
    Offset 15,15

    Blend SrcAlpha OneMinusSrcAlpha 

    CGPROGRAM
    #pragma vertex vert
    #pragma exclude_renderers gles xbox360 ps3
    ENDCG
    SetTexture[_MainTex] { combine primary }
}

CGPROGRAM
#pragma surface surf Lambert
struct Input {
    float2 uv_MainTex;
    float2 uv_BumpMap;
};
sampler2D _MainTex;
sampler2D _BumpMap;
uniform float3 _Color;
void surf(Input IN, inout SurfaceOutput o) {
    o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb * _Color;
    o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
}
ENDCG

}

Fallback "Outlined/Silhouetted Diffuse"
}