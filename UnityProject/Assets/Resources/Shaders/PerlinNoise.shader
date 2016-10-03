Shader "Noise/PerlinNoise"
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
            
            #include "UnityCG.cginc"

            sampler2D _ColorTexture;

            sampler2D _PermutationTable;
            sampler2D _Gradient4Table;

            int _Octaves;
            float _Gain;
            float _Lacunarity;
            float _TimeMultiplier;

            // From: http://http.download.nvidia.com/developer/GPU_Gems_2/CD/Index.html
            float3 fade(float3 t)
            {
                return t * t * t * (t * (t * 6 - 15) + 10);
            }

            float4 fade(float4 t)
            {
                return t * t * t * (t * (t * 6 - 15) + 10);
            }

            float perm(float x)
            {
                return tex2D(_PermutationTable, float2(x,0));
            }

            // 4d gradient
            float grad(float x, float4 p)
            {
                return dot(tex2D(_Gradient4Table, float2(x, 0.)), p);
            }

            // 4D noise
            float inoise(float4 p)
            {
                float4 P = fmod(floor(p), 256.0);   // FIND UNIT HYPERCUBE THAT CONTAINS POINT
                p -= floor(p);                      // FIND RELATIVE X,Y,Z OF POINT IN CUBE.
                float4 f = fade(p);                 // COMPUTE FADE CURVES FOR EACH OF X,Y,Z, W
                P = P / 256.0;
                const float one = 1.0 / 256.0;

                // HASH COORDINATES OF THE 16 CORNERS OF THE HYPERCUBE
                float A = perm(P.x) + P.y;
                float AA = perm(A) + P.z;
                float AB = perm(A + one) + P.z;
                float B = perm(P.x + one) + P.y;
                float BA = perm(B) + P.z;
                float BB = perm(B + one) + P.z;

                float AAA = perm(AA) + P.w, AAB = perm(AA + one) + P.w;
                float ABA = perm(AB) + P.w, ABB = perm(AB + one) + P.w;
                float BAA = perm(BA) + P.w, BAB = perm(BA + one) + P.w;
                float BBA = perm(BB) + P.w, BBB = perm(BB + one) + P.w;

                // INTERPOLATE DOWN
                float ret = lerp(
                    lerp(lerp(lerp(grad(perm(AAA), p),
                        grad(perm(BAA), p + float4(-1, 0, 0, 0)), f.x),
                        lerp(grad(perm(ABA), p + float4(0, -1, 0, 0)),
                            grad(perm(BBA), p + float4(-1, -1, 0, 0)), f.x), f.y),

                        lerp(lerp(grad(perm(AAB), p + float4(0, 0, -1, 0)),
                            grad(perm(BAB), p + float4(-1, 0, -1, 0)), f.x),
                            lerp(grad(perm(ABB), p + float4(0, -1, -1, 0)),
                                grad(perm(BBB), p + float4(-1, -1, -1, 0)), f.x), f.y), f.z),

                    lerp(lerp(lerp(grad(perm(AAA + one), p + float4(0, 0, 0, -1)),
                        grad(perm(BAA + one), p + float4(-1, 0, 0, -1)), f.x),
                        lerp(grad(perm(ABA + one), p + float4(0, -1, 0, -1)),
                            grad(perm(BBA + one), p + float4(-1, -1, 0, -1)), f.x), f.y),

                        lerp(lerp(grad(perm(AAB + one), p + float4(0, 0, -1, -1)),
                            grad(perm(BAB + one), p + float4(-1, 0, -1, -1)), f.x),
                            lerp(grad(perm(ABB + one), p + float4(0, -1, -1, -1)),
                                grad(perm(BBB + one), p + float4(-1, -1, -1, -1)), f.x), f.y), f.z), f.w);
                return ret;
            }

            float2 fBm(float4 p, int octaves, float lacunarity, float gain)
            {
                float freq = 1.0;
                float amp = 0.5;
                float range = 0.;
                float sum = 0;
                for (int i = 0; i<octaves; i++) {
                    sum += inoise(p*freq)*amp;
                    range += amp;
                    freq *= lacunarity;
                    amp *= gain;
                }
                return float2(sum, range);
            }

            float toZeroOne(float x, float range)
            {
                return ((x / range) + 1) * .5;
            }

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float4 lookup = float4(i.worldPos.xyz, _Time.x * _TimeMultiplier);
                float2 noiseAndRange = fBm(lookup, _Octaves, _Lacunarity, _Gain);
                float noise = toZeroOne(noiseAndRange.x, noiseAndRange.y);
                if (noise < 0.)
                    return half4(1., 0., 1., 0.);
                return tex2D(_ColorTexture, float2(noise, 0.));
            }
            ENDCG
        }
    }
}
