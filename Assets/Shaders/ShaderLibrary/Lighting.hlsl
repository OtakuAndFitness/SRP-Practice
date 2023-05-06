#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

//计算入射光照
float3 IncomingLight(Surface sf, Light light)
{
    return saturate(dot(sf.normal,light.direction) * light.attenuation) * light.color;
}

//入射光乘以光照照射到表面的直接照明颜色,得到最终的照明颜色
float3 GetLighting(Surface sf, BRDF brdf, Light light)
{
    return IncomingLight(sf,light) * DirectBRDF(sf,brdf,light);
}

//根据物体的表面信息和灯光属性获取最终光照结果
float3 GetLighting(Surface surfaceWS, BRDF brdf)
{
    float3 color = 0.0;
    for (int i=0;i< GetLightCounts();i++)
    {
        color += GetLighting(surfaceWS, brdf, GetDirectionalLight(i, surfaceWS));
    }
    return color;
}



#endif