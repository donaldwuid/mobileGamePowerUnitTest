Shader "Unlit/S_GPUTest"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f input) : SV_Target
            {
                // sample the texture
                half4 tex = tex2D(_MainTex, input.uv);
                half4 col = _Time * input.uv.x;

                {
                    UNITY_UNROLL
                    for (int i = 0; i < 340; ++i)
                    {
                        col += col * col + col;
                    }
                }
                {
                    UNITY_UNROLL
                    for (int i = 0; i < 340; ++i)
                    {
                        col -= col * col + col;
                    }
                }

                return tex + saturate(col);
            }
            ENDCG
        }
    }
}
