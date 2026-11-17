// Cielo de Kibo el Origen: degradado de día/noche, sol, luna, nubes que se mueven y estrellas que titilan.
Shader "Kibo/Cielo"
{
    Properties
    {
        _DiaArriba ("Día arriba", Color) = (0.25, 0.53, 0.93, 1)
        _DiaHorizonte ("Día horizonte", Color) = (0.74, 0.88, 1.0, 1)
        _NocheArriba ("Noche arriba", Color) = (0.01, 0.015, 0.06, 1)
        _NocheHorizonte ("Noche horizonte", Color) = (0.07, 0.09, 0.2, 1)
        _Atardecer ("Atardecer", Color) = (1.0, 0.55, 0.3, 1)
        _Suelo ("Suelo", Color) = (0.36, 0.45, 0.34, 1)
        _Noche ("Noche (0 = día, 1 = noche)", Range(0, 1)) = 0
        _SolDir ("Dirección hacia el sol", Vector) = (0, 0.6, 0.8, 0)
        _Nubes ("Cantidad de nubes", Range(0, 1)) = 0.55
        _VelNubes ("Velocidad de las nubes", Float) = 0.012
        _TamNubes ("Tamaño de las nubes", Float) = 1.6
        _Estrellas ("Brillo de las estrellas", Range(0, 3)) = 1.3
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            fixed4 _DiaArriba, _DiaHorizonte, _NocheArriba, _NocheHorizonte, _Atardecer, _Suelo;
            float _Noche, _Nubes, _VelNubes, _TamNubes, _Estrellas;
            float4 _SolDir;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            float hash2(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float hash3(float3 p) { return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453); }

            float ruido(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash2(i);
                float b = hash2(i + float2(1, 0));
                float c = hash2(i + float2(0, 1));
                float d = hash2(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int k = 0; k < 5; k++)
                {
                    v += a * ruido(p);
                    p = p * 2.03 + float2(17.1, 9.2);
                    a *= 0.5;
                }
                return v;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = d.y;
                float n = saturate(_Noche);

                // Degradado del cielo
                float3 arriba = lerp(_DiaArriba.rgb, _NocheArriba.rgb, n);
                float3 horiz = lerp(_DiaHorizonte.rgb, _NocheHorizonte.rgb, n);
                float3 col = lerp(horiz, arriba, pow(saturate(h), 0.45));

                // Atardecer: tono naranja cerca del horizonte en la transición
                float3 s = normalize(_SolDir.xyz + float3(0, 0.0001, 0));
                float trans = saturate(1.0 - abs(n * 2.0 - 1.0));
                float haciaSol = saturate(dot(normalize(float3(d.x, 0, d.z) + 0.0001), normalize(float3(s.x, 0, s.z) + 0.0001)) * 0.5 + 0.5);
                col += _Atardecer.rgb * pow(saturate(1.0 - abs(h)), 4.0) * trans * (0.35 + 0.65 * haciaSol) * 0.7;

                // Sol
                float sd = saturate(dot(d, s));
                col += float3(1.0, 0.95, 0.8) * (smoothstep(0.9988, 0.9994, sd) * 2.5 + pow(sd, 250.0) * 0.5) * (1.0 - n);

                // Luna (en el lado opuesto al sol)
                float ld = saturate(dot(d, -s));
                col += float3(0.85, 0.9, 1.0) * smoothstep(0.9991, 0.9995, ld) * n * 1.3;
                col += float3(0.25, 0.3, 0.45) * pow(ld, 120.0) * n * 0.6;

                // Estrellas
                if (h > 0.0)
                {
                    float3 g = d * 190.0;
                    float3 celda = floor(g);
                    float r = hash3(celda);
                    float3 f = frac(g) - 0.5;
                    float estrella = step(0.982, r) * smoothstep(0.36, 0.0, length(f));
                    float titila = 0.55 + 0.45 * sin(_Time.y * (2.0 + r * 3.0) + r * 120.0);
                    col += estrella * titila * n * n * _Estrellas * saturate(h * 5.0);
                }

                // Nubes
                if (h > 0.0)
                {
                    float2 uv = d.xz / (h + 0.15);
                    uv = uv * _TamNubes + _Time.y * _VelNubes * float2(1.0, 0.35);
                    float c = fbm(uv);
                    float cobertura = 1.0 - _Nubes;
                    float nube = smoothstep(cobertura, cobertura + 0.22, c) * saturate(h * 6.0);
                    float denso = smoothstep(cobertura + 0.1, cobertura + 0.45, c);
                    float3 colNube = lerp(float3(1.0, 1.0, 1.0), float3(0.13, 0.15, 0.24), n);
                    colNube = lerp(colNube, colNube * 0.78, denso);
                    colNube += _Atardecer.rgb * trans * 0.25;
                    col = lerp(col, colNube, nube * 0.92 * (1.0 - 0.45 * n));
                }

                // Debajo del horizonte
                float3 suelo = lerp(_Suelo.rgb, _Suelo.rgb * 0.12, n);
                col = lerp(col, suelo, smoothstep(0.0, -0.06, h));

                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
