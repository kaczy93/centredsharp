#define NONE 0
#define HUED 1
#define PARTIAL 2

static const float TileSize = 31.11;

sampler TextureSampler : register(s0);
sampler HueSampler : register(s1);

cbuffer ProjectionMatrix : register(b0) {
    float4x4 WorldViewProj;
};

cbuffer Hues : register(b1) {
    int HueCount;
}

cbuffer VirtualLayer : register(b2) {
    float4 VirtualLayerFillColor;
    float4 VirtualLayerBorderColor;
};

/* For now, all the techniques use the same vertex definition */
struct VSInput {
    float4 Position : SV_Position;
    float3 TexCoord : TEXCOORD0;
    float3 HueCoord : TEXCOORD1;
};

struct VSOutput {
    float4 OutputPosition : SV_Position;
    float3 TexCoord       : TEXCOORD0;
    float3 HueCoord       : TEXCOORD1;
};

struct PSInput {
    float3 TexCoord       : TEXCOORD0;
    float3 HueCoord       : TEXCOORD1;
};

bool is_zero_vector(float3 v)
{   
    return v.x == 0 && v.y == 0 && v.z == 0;
}

//Common vertex shader
VSOutput TileVSMain(VSInput vin) {
    VSOutput vout;

    vout.OutputPosition = mul(vin.Position, WorldViewProj);
    vout.OutputPosition.z += vin.TexCoord.z;
    vout.TexCoord = vin.TexCoord;
    vout.HueCoord = vin.HueCoord;

    return vout;
}

float4 TerrainPSMain(PSInput pin) : SV_Target0
{
    float4 color = tex2D(TextureSampler, pin.TexCoord.xy);
    if (color.a == 0)
        discard;
        
//    float3 normal = pin.HueCoord;
        
    return color;
}

float4 StaticsPSMain(PSInput pin) : SV_Target0
{
    float4 color = tex2D(TextureSampler, pin.TexCoord.xy);
    if (color.a == 0)
        discard;
        
    int mode = int(pin.HueCoord.y);
        
    if (mode == HUED || (mode == PARTIAL && color.r == color.g && color.r == color.b))
    {
        float2 hueCoord = float2(color.r, pin.HueCoord.x / HueCount);
        color.rgb = tex2D(HueSampler, hueCoord).rgb;
    }

    color.a = pin.HueCoord.z;

    return color;
}

float4 SelectionPSMain(PSInput pin) : SV_Target0
{
    float4 color = tex2D(TextureSampler, pin.TexCoord.xy);
     if (color.a == 0)
            discard;
    return float4(pin.HueCoord, 1.0);
}

VSOutput VirtualLayerVSMain(VSInput vin) {
    VSOutput vout;
    
    vout.OutputPosition = mul(vin.Position, WorldViewProj);
    vout.TexCoord = vin.Position;
    vout.HueCoord = vin.HueCoord;
    
    return vout;
}

float4 VirtualLayerPSMain(PSInput pin) : SV_Target0
{
    //0.7 worked for me as it's not glitching when moving camera
    if (abs(fmod(pin.TexCoord.x, TileSize)) < 0.7 || abs(fmod(pin.TexCoord.y, TileSize)) < 0.7) 
    {
            return VirtualLayerBorderColor;
    } 
    else 
    {
            return VirtualLayerFillColor;
    }
}

Technique Terrain
{
    Pass
    {
        VertexShader = compile vs_2_0 TileVSMain();
        PixelShader = compile ps_2_0 TerrainPSMain();
    }
}

Technique Statics {
    Pass
    {
        VertexShader = compile vs_2_0 TileVSMain();
        PixelShader = compile ps_2_0 StaticsPSMain();
    }
}

Technique Selection {
    Pass
    {
        VertexShader = compile vs_2_0 TileVSMain();
        PixelShader = compile ps_2_0 SelectionPSMain();
    }
}

Technique VirtualLayer {
    Pass
    {
        VertexShader = compile vs_2_0 VirtualLayerVSMain();
        PixelShader = compile ps_2_0 VirtualLayerPSMain();
    }
}