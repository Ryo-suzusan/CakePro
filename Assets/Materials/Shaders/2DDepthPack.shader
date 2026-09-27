Shader "Custom/2DDepthPack"
{
Properties
    {
        // URPのフルスクリーンパスは、直前までのゲーム画面を _BlitTexture という名前で受け取ります
        [HideInInspector] _BlitTexture("Blit Texture", 2D) = "white" {}
        // Depthカメラが書き込んだRenderTextureを受け取る窓口
        _CustomDepthTex("Custom Depth Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "2DDepthPackPass"
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv   : TEXCOORD0;
            };

            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            TEXTURE2D(_CustomDepthTex);
            SAMPLER(sampler_CustomDepthTex);

            Varyings Vert(Attributes input)
            {
                Varyings output;
                // 画面全体に三角形を広げるURPの標準関数
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // 画面の左半分 (UV.x が 0.0 ~ 0.5)は、RGB映像
                if (uv.x < 0.5)
                {
                    // 横方向を2倍に引き伸ばして、通常のゲーム画面（RGB）をサンプリング
                    float2 colorUV = float2(uv.x * 2.0, uv.y);
                    return SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, colorUV);
                }
                // 画面の右半分 (UV.x が 0.5 ~ 1.0)は、深度映像
                else
                {
                    // 横方向を2倍に引き伸ばして、深度用のUVを計算
                    float2 depthUV = float2((uv.x - 0.5) * 2.0, uv.y);
                    
                    // RenderTextureから深度をサンプリング
                    float rawDepth = SAMPLE_TEXTURE2D_X_LOD(_CustomDepthTex, sampler_CustomDepthTex, depthUV, 0).r;
                    
                    // DepthをRチャンネルに出力
                    return float4(rawDepth * 10, 0, 0, 1.0);
                }
            }
            ENDHLSL
        }
    }
}