#ifndef ANIME_LIGHTING_INCLUDED
#define ANIME_LIGHTING_INCLUDED

// The lighting every lit pack shader shares: an analytic two-tone ramp with the main light's cast shadow joined to
// it, the clouds' shadows dimming its lit tone softly, an optional second thin shadow band, additional lights through
// the same ramp, a fresnel rim, emission, and a gentle ambient. The ramp is the single seam (AnimeToneWeight and its callers): a ramp texture could replace it
// later without touching a caller.

#include "AnimeSurface.hlsl"
#include "AnimeDirector.hlsl"
#include "AnimeClouds.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// 0 below the threshold, 1 above it, smooth across softness (a width in ramp units, never zero).
half AnimeToneWeight(half x, half threshold, half softness)
{
    half halfWidth = max(softness, 0.004h) * 0.5h;
    return smoothstep(threshold - halfWidth, threshold + halfWidth, x);
}

// Half-lambert: 0.5 is the terminator, so a threshold of 0.5 splits lit from shadow at N.L = 0.
half AnimeHalfLambert(half3 normalWS, half3 lightDirectionWS)
{
    return dot(normalWS, lightDirectionWS) * 0.5h + 0.5h;
}

// How lit the main light's cast shadow leaves a pixel, 0 to 1: its edge sits where the shadow map's attenuation crosses
// one half, as hard or soft as the ramp's softness, so the cast edge reads like the tone edge whatever the threshold.
half AnimeCastLit(half shadowAttenuation)
{
    return AnimeToneWeight(shadowAttenuation, 0.5h, _ShadowSoftness);
}

// How strongly cast shadows darken: the material's strength times the director's, faded out toward the shadow
// distance (URP's own fade), so a shadow thins smoothly instead of snapping off.
half AnimeCastStrength(float3 positionWS)
{
    half strength = saturate(_CastShadowStrength * AnimeDirectorShadowStrengthMultiplier());
#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    strength *= 1.0h - GetMainLightShadowFade(positionWS);
#endif
    return strength;
}

// Where the main light's shadow is read. Per surface: the pixel itself. As a whole body: one point for the whole
// renderer, its bounds' centre moved _BodyShadowReach toward the light, outside the body, so the body's own parts
// (a hat, hair, an arm) never shade it and nothing crawls as it moves, while scenery between it and the light still
// does. Screen-space shadows have no shadow map to read at another point: there the surface is used.
float3 AnimeShadowPoint(float3 positionWS, half3 lightDirectionWS)
{
#if defined(ANIME_GROUP_BODY) && !defined(_MAIN_LIGHT_SHADOWS_SCREEN)
    if (ANIME_BODY_SHADOW_ON)
    {
        float3 centre = (unity_RendererBounds_Min.xyz + unity_RendererBounds_Max.xyz) * 0.5;
        return GetCameraRelativePositionWS(centre) + float3(lightDirectionWS) * _BodyShadowReach;
    }
#endif
    return positionWS;
}

// The main light's cast shadow, 0 (shadowed) to 1 (lit), and how strongly it darkens (strength). A whole body's
// shadow follows the shadow map's own filtering, so it fades smoothly as the body walks into shade; a surface's takes
// the ramp's edge.
half AnimeMainLightCast(InputData inputData, half3 lightDirectionWS, out half strength)
{
    float3 shadowPoint = AnimeShadowPoint(inputData.positionWS, lightDirectionWS);
    float4 shadowCoord = inputData.shadowCoord;
#if defined(MAIN_LIGHT_CALCULATE_SHADOWS) && !defined(_MAIN_LIGHT_SHADOWS_SCREEN)
    if (ANIME_BODY_SHADOW_ON)
        shadowCoord = TransformWorldToShadowCoord(shadowPoint);
#endif
    half attenuation = MainLightRealtimeShadow(shadowCoord);
    strength = AnimeCastStrength(shadowPoint);
    return ANIME_BODY_SHADOW_ON ? attenuation : AnimeCastLit(attenuation);
}

// How much the clouds shade a pixel from the main light, 0 to the published strength times the material's response:
// read once for a whole body at its shadow point (so a cloud's edge never slices a character), per pixel otherwise. Soft
// by the cloud's own edge, not the ramp's, and not faded by the shadow distance. Off (the keyword), nothing.
half AnimeCloudShade(float3 positionWS, half3 lightDirectionWS)
{
    half shade = 0.0h;
#if defined(_ANIME_CLOUD_SHADOWS_KEYWORD_DECLARED) || defined(_ANIME_CLOUD_SHADOWS)
    UNITY_BRANCH
    if (ANIME_CLOUD_SHADOWS_ON)
    {
        float3 towardSun = AnimeDirectorLightDirection(lightDirectionWS);
        shade = AnimeCloudShadow(AnimeShadowPoint(positionWS, lightDirectionWS), towardSun) * _CloudShadow;
    }
#endif
    return shade;
}

half3 AnimeProbeAmbient()
{
    return max(half3(0, 0, 0), half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w));
}

bool AnimeLightMatchesMesh(uint lightLayerMask)
{
#if defined(_LIGHT_LAYERS)
    return IsMatchingLightLayer(lightLayerMask, GetMeshRenderingLayer());
#else
    return true;
#endif
}

half3 AnimeExtraLightTerm(AnimeSurface surface, Light light)
{
    if (!AnimeLightMatchesMesh(light.layerMask))
        return half3(0, 0, 0);
    half x = AnimeHalfLambert(surface.normalWS, light.direction);
    half weight = AnimeToneWeight(x, _ShadowThreshold, _ShadowSoftness);
    return light.color * (weight * light.distanceAttenuation * light.shadowAttenuation);
}

// Additional lights through the ramp, scaled and capped (per channel, relative to the albedo).
half3 AnimeExtraLights(AnimeSurface surface, InputData inputData, half4 shadowMask)
{
    half3 sum = half3(0, 0, 0);
#if defined(_ANIME_EXTRA_LIGHTS) && defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_CLUSTER_LIGHT_LOOP
    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
        Light light = GetAdditionalLight(lightIndex, inputData.positionWS, shadowMask);
        sum += AnimeExtraLightTerm(surface, light);
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData.positionWS, shadowMask);
        sum += AnimeExtraLightTerm(surface, light);
    LIGHT_LOOP_END

    sum = min(sum * _ExtraLightStrength, _ExtraLightCap);
    sum *= surface.albedo;
#endif
    return sum;
}

#if defined(ANIME_GROUP_RIM)
half3 AnimeRim(half3 normalWS, half3 viewDirectionWS, half litWeight)
{
    half fresnel = 1.0h - saturate(dot(normalWS, viewDirectionWS));
    half rim = AnimeToneWeight(fresnel, 1.0h - _RimWidth, _RimSoftness);
    rim *= lerp(1.0h, litWeight, _RimLightMask);
    return _RimColor.rgb * (rim * _RimStrength * AnimeDirectorRimStrengthMultiplier());
}
#endif

// The finished colour of a surface (fog not applied). inputData needs positionWS, normalWS, viewDirectionWS,
// shadowCoord and normalizedScreenSpaceUV.
half3 AnimeShade(AnimeSurface surface, InputData inputData)
{
    half4 shadowMask = half4(1, 1, 1, 1);
    Light mainLight = GetMainLight();
    bool mainMatches = AnimeLightMatchesMesh(mainLight.layerMask);

    half3 lightDirection = half3(AnimeDirectorLightDirection(mainLight.direction));
    half x = AnimeHalfLambert(surface.normalWS, lightDirection);
    half formWeight = AnimeToneWeight(x, _ShadowThreshold, _ShadowSoftness);
    half castStrength;
    half castLit = AnimeMainLightCast(inputData, half3(mainLight.direction), castStrength);
    half litWeight = lerp(formWeight, min(formWeight, castLit), castStrength);
    litWeight *= 1.0h - AnimeCloudShade(inputData.positionWS, half3(mainLight.direction));
    litWeight = mainMatches ? litWeight : 0.0h;

    half3 lightColor = mainMatches ? mainLight.color * AnimeDirectorLightColorMultiplier() : half3(0, 0, 0);
    half3 litTone = surface.albedo * lerp(half3(1, 1, 1), lightColor, _LightInfluence);

    half3 ambient = AnimeDirectorAmbient(AnimeProbeAmbient());
    half3 shadowTone = surface.albedo * (_ShadowTint.rgb * AnimeDirectorShadowColorMultiplier() + ambient * _AmbientStrength);

#if defined(ANIME_GROUP_BAND)
    if (ANIME_BAND2_ON)
    {
        half deepWeight = AnimeToneWeight(x, _ShadowThreshold - _Band2Offset, _Band2Softness);
        shadowTone = lerp(shadowTone * _Band2Tint.rgb, shadowTone, deepWeight);
    }
#endif

    half3 color = lerp(shadowTone, litTone, litWeight);
    color += AnimeExtraLights(surface, inputData, shadowMask);

#if defined(ANIME_GROUP_RIM)
    if (ANIME_RIM_ON)
        color += AnimeRim(surface.normalWS, inputData.viewDirectionWS, litWeight);
#endif

    return color + surface.emission;
}

#endif
