Shader "Custom/BoneShader"
{
    Properties
    {
        _Color ("Color", Color) = (0, 1, 0, 1)
    }
    SubShader
    {
        Tags { "Queue" = "Overlay" "RenderType"="Opaque" }
        Pass
        {
            Cull Off           // Render both sides (optional)
            ZWrite Off         // Don't write to depth buffer
            ZTest Always       // Always render in front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 _Color;
            float4 frag(v2f i) : SV_Target
            {
                return _Color; // Solid color
            }
            ENDHLSL
        }
    }
}
