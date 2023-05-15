#ifndef CUSTOM_UNITY_BRDF_INCLUDED
#define CUSTOM_UNITY_BRDF_INCLUDED

#define MIN_REFLECTIVITY 0.04

struct BRDF
{
    float3 diffuse;
    float3 specular;
    float roughness;
    float perceptualRoughness;
    float fresnel;
};

float OneMinusReflectivity(float metallic)
{
    float range = 1.0 - MIN_REFLECTIVITY;
    return range - metallic * range;
    
}

float SpecularStrength(Surface sf, BRDF brdf, Light light)
{
    float3 h = SafeNormalize(light.direction + sf.viewDir);
    float nh2 = Square(saturate(dot(sf.normal,h)));
    float lh2 = Square(saturate(dot(light.direction,h)));
    float r2 = Square(brdf.roughness);
    float d2 = Square(nh2 * (r2-1.0) + 1.00001);
    float normalization = brdf.roughness * 4.0 + 2.0;
    return r2 / (d2 * max(0.1, lh2) * normalization);
}

float3 DirectBRDF(Surface sf, BRDF brdf, Light light)
{
    return SpecularStrength(sf,brdf,light) * brdf.specular + brdf.diffuse;
}

float3 IndirectBRDF(Surface sf, BRDF brdf, float3 diffuse, float3 specular)
{
    float fresnelStrength = sf.fresnelStrength * Pow4(1.0 - saturate(dot(sf.normal, sf.viewDir)));
    float3 reflection = specular * lerp(brdf.specular, brdf.fresnel, fresnelStrength);
    reflection /= brdf.roughness * brdf.roughness + 1.0;
    return diffuse * brdf.diffuse + reflection;
}

BRDF GetBRDF(Surface sf, bool applyAlphaToDiffuse = false)
{
    BRDF brdf;
    float oneMinusReflectivity = OneMinusReflectivity(sf.metallic);
    brdf.diffuse = sf.color * oneMinusReflectivity;
    if ( applyAlphaToDiffuse)
    {
        //透明度预乘
        brdf.diffuse *= sf.alpha;
    }
    
    brdf.specular = lerp(MIN_REFLECTIVITY,sf.color,sf.metallic);
    brdf.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(sf.smoothness);
    brdf.roughness = PerceptualRoughnessToRoughness(brdf.perceptualRoughness);
    brdf.fresnel = saturate(sf.smoothness + 1.0 - oneMinusReflectivity);
    return brdf;
}

#endif