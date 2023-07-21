sampler TextureSampler : register(s0);

cbuffer ProjectionMatrix : register(b0) {
    float4x4 WorldViewProj        : register(vs, c0);
};

struct VSInput {
    float4 Position : SV_Position;
    float2 TexCoord : TEXCOORD0;
};

struct VSOutput {
    float4 Position           : SV_Position;
    float4 ScreenPosition     : TEXCOORD0;
    float2 TexCoord           : TEXCOORD1;
};

struct PSInput {
    float4 ScreenPosition     : TEXCOORD0;
    float2 TexCoord           : TEXCOORD1;
};

VSOutput VSMain(VSInput vin) {
    VSOutput vout;

    vout.Position = mul(vin.Position, WorldViewProj);
    vout.ScreenPosition = vout.Position;
    vout.TexCoord = vin.TexCoord;

    return vout;
}


float4 PSMain(PSInput pin) : SV_Target0
{
    float4 color = tex2D(TextureSampler, pin.TexCoord);

    if (color.a == 0)
        discard;

    color.r = pin.ScreenPosition.z;
    color.g = 0;
    color.b = 0;
    // We leave alpha in case we want to use that when generating the shadows

    return color;
}

Technique BasicEffect {
    Pass
    {
        VertexShader = compile vs_2_0 VSMain();
        PixelShader = compile ps_2_0 PSMain();
    }
}
