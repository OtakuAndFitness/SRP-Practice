#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

//入射光乘以表面颜色，得到最终照明颜色
float3 GetIncomingLight(Surface sf, Light light)
{
    return saturate(dot(sf.normal,light.direction)) * light.color;
}

//获取最终照明结果
float3 GetLighting(Surface sf)
{
    float3 color = 0.0;
    for (int i=0;i< GetLightCounts();i++)
    {
        color += GetIncomingLight(sf,GetDirectionalLight(i));
    }
    return color;
}



#endif