Shader "Custom/AOShader"
{
    Properties
    { 
        [HideInInspector] _Color ("Color", Color) = (1,1,1,1)
        [NoScaleOffset] _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.35
        _Metallic ("Metallic", Range(0,1)) = 0.0
        [MaterialToggle] _Emission("Emmission", Float) = 0
        [HideInInspector] [HDR] _EmissionColor("Emission", Color) = (1,1,1)
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _DoubleSided("Culling", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Cull [_DoubleSided]
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard addshadow alphatest:_Cutoff
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        fixed4 _EmissionColor;		
        float _Emission;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Emission = c.rgba * tex2D(_MainTex, IN.uv_MainTex).a * _EmissionColor * lerp(0, 1, _Emission);
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
