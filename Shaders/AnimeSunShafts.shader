Shader "Hidden/AnimeShaders/SunShafts"
{
    // The sun shafts' passes, drawn by AnimeSunShaftsFeature. 0: the mask (at half resolution: the sky's colour above the
    // threshold where all
    // four full-resolution depths under the texel are the far plane, so a thin occluder still blocks, weighted by its
    // nearness to the sun on screen). 1, 2: the radial blur toward the sun (the first long, the second short to fill the
    // gaps). 3: the composite, added over the camera colour in the sun's colour.
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        // x, y: the sun's place on screen (0 to 1)   z: how far round it the sky gives off shafts   w: the screen's aspect
        float4 _SunShaftsSun;
        // x: the long blur's length   y: the decay along a shaft   z: samples   w: the short blur's length
        float4 _SunShaftsBlur;
        // rgb: the shafts' colour (the sun's, the tint, the intensity, the fade by angle)   a: the brightness threshold
        float4 _SunShaftsColor;

        bool AnimeShaftsFar(float rawDepth)
        {
        #if UNITY_REVERSED_Z
            return rawDepth <= 1e-6;
        #else
            return rawDepth >= 1.0 - 1e-6;
        #endif
        }

        half4 AnimeShaftsMask(Varyings input) : SV_Target
        {
            float2 uv = input.texcoord;
            float2 texel = 0.5 / float2(_ScreenParams.xy);
            bool sky = AnimeShaftsFar(SampleSceneDepth(uv + float2(-texel.x, -texel.y)))
                && AnimeShaftsFar(SampleSceneDepth(uv + float2(texel.x, -texel.y)))
                && AnimeShaftsFar(SampleSceneDepth(uv + float2(-texel.x, texel.y)))
                && AnimeShaftsFar(SampleSceneDepth(uv + float2(texel.x, texel.y)));
            float2 toSun = (uv - _SunShaftsSun.xy) * float2(_SunShaftsSun.w, 1.0);
            float near = exp(-dot(toSun, toSun) / max(_SunShaftsSun.z * _SunShaftsSun.z, 1e-4));
            half3 colour = max(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb - half(_SunShaftsColor.a), 0.0h);
            return half4(colour * half(near) * (sky ? 1.0h : 0.0h), 1.0h);
        }

        half4 AnimeShaftsBlur(float2 uv, float length)
        {
            int count = (int)max(_SunShaftsBlur.z, 1.0);
            float2 step = (_SunShaftsSun.xy - uv) * length / count;
            half3 sum = half3(0, 0, 0);
            half weight = 1.0h, total = 0.0h;
            for (int i = 0; i < count; i++)
            {
                sum += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + step * i, 0).rgb * weight;
                total += weight;
                weight *= half(_SunShaftsBlur.y);
            }
            return half4(sum / max(total, 1e-4h), 1.0h);
        }

        half4 AnimeShaftsBlurLong(Varyings input) : SV_Target
        {
            return AnimeShaftsBlur(input.texcoord, _SunShaftsBlur.x);
        }

        half4 AnimeShaftsBlurShort(Varyings input) : SV_Target
        {
            return AnimeShaftsBlur(input.texcoord, _SunShaftsBlur.w);
        }

        half4 AnimeShaftsComposite(Varyings input) : SV_Target
        {
            half3 shafts = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
            return half4(shafts * half3(_SunShaftsColor.rgb), 1.0h);
        }
        ENDHLSL

        Pass
        {
            Name "Mask"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment AnimeShaftsMask
            ENDHLSL
        }

        Pass
        {
            Name "BlurLong"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment AnimeShaftsBlurLong
            ENDHLSL
        }

        Pass
        {
            Name "BlurShort"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment AnimeShaftsBlurShort
            ENDHLSL
        }

        Pass
        {
            Name "Composite"
            Blend One One
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment AnimeShaftsComposite
            ENDHLSL
        }
    }

    Fallback Off
}
