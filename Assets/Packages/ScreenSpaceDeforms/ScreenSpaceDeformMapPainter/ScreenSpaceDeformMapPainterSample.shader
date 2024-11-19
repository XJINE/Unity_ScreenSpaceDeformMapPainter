Shader "Unlit/ScreenSpaceDeformMapPainterSample"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM

            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.xjine.screen_space_deform_shader/ScreenSpaceDeformLibrary.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
            };

            sampler2D _MainTex;

            v2f vert (appdata v)
            {
                v2f o;

                o.vertex = ScreenSpaceDeform(v.vertex);
                o.uv     = v.uv;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }

            ENDCG
        }
    }
}
