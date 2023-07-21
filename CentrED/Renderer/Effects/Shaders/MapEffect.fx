sampler TextureSampler : register(s0);
sampler ShadowSampler : register(s1);

cbuffer Parameters : register(b0) {
    float4 AmbientLightColor               : register(vs, c0)  : register(ps, c1);

    float3 DirectionalLightPosition        : register(vs, c1)  : register(ps, c2);
    float3 DirectionalLightDirection       : register(vs, c2)  : register(ps, c3);
    float3 DirectionalLightDiffuseColor    : register(vs, c3)  : register(ps, c4);
    float3 DirectionalLightSpecularColor   : register(vs, c4)  : register(ps, c5);
};

cbuffer ProjectionMatrix : register(b1) {
    float4x4 WorldViewProj            : register(vs, c5);
    float4x4 LightWorldViewProj       : register(vs, c9);
};

struct VSInput {
    float4 Position : SV_Position;
    float3 Normal   : NORMAL;
    float2 TexCoord : TEXCOORD0;
};

struct VSOutput {
    float4 ScreenPosition       : SV_Position;
    float4 WorldPosition        : TEXCOORD0;
    float4 LightViewPosition    : TEXCOORD1;
    float2 TexCoord             : TEXCOORD2;
    float3 Normal               : TEXCOORD3;
};

struct PSInput {
    float4 WorldPosition        : TEXCOORD0;
    float4 LightViewPosition    : TEXCOORD1;
    float2 TexCoord             : TEXCOORD2;
    float3 Normal               : TEXCOORD3;
};

VSOutput VSMain(VSInput vin) {
    VSOutput vout;

    vout.ScreenPosition = mul(vin.Position, WorldViewProj);
    vout.WorldPosition = vin.Position;
    vout.LightViewPosition = mul(vin.Position, LightWorldViewProj);
    vout.Normal = vin.Normal;
    vout.TexCoord = vin.TexCoord;
    
    return vout;
}

float4 PSMain(PSInput pin) : SV_Target0
{
    float4 color = tex2D(TextureSampler, pin.TexCoord.xy);

    if (color.a == 0)
        discard;

    float2 LightViewTexCoords;
    LightViewTexCoords.x = (pin.LightViewPosition.x / pin.LightViewPosition.w) / 2.0f + 0.5f;
    LightViewTexCoords.y = (-pin.LightViewPosition.y / pin.LightViewPosition.w) / 2.0f + 0.5f; //zero y in a texture is the top, so reverse coordinates

    // get depth from shadow map red channel
    float lightViewDepth = tex2D(ShadowSampler, LightViewTexCoords).r;

    // compare shadow-map depth to the actual depth
    // subtracting a small value helps avoid floating point equality errors (depth bias)
    // when the distances are equal
    float pixelDepth = pin.LightViewPosition.z / pin.LightViewPosition.w;
    float bias = max(0.012f * (1.0f - dot(DirectionalLightDirection, normalize(pin.Normal))), 0.01f);
    //float bias = 0.005f;
    if (pixelDepth - bias > lightViewDepth) {
        // In shadow. Darken the color.
        color.rgb *= 0.5f;
    }

    float3 dotL = mul(-DirectionalLightDirection, normalize(pin.Normal));
    float3 diffuse = step(0, dotL) * dotL;

    color.rgb *= mul(diffuse, DirectionalLightDiffuseColor) * AmbientLightColor.rgb;
    color.rgb += DirectionalLightSpecularColor * color.a;

    return color;
}

Technique BasicEffect
{
    Pass
    {
        VertexShader = compile vs_2_0 VSMain();
        PixelShader = compile ps_2_0 PSMain();
    }
}
