#define NONE 0
#define HUED 1
#define PARTIAL 2

static const float HuesPerTexture = 3000;
static const float TileSize = 31.11;

sampler TextureSampler : register(s0);
sampler HueSampler : register(s1);

cbuffer ProjectionMatrix : register(b0) {
    float4x4 WorldViewProj;
};

cbuffer VirtualLayer : register(b1) {
    float4 VirtualLayerFillColor;
    float4 VirtualLayerBorderColor;
};


/* For now, all the techniques use the same vertex definition */
struct VSInput {
    float4 Position : SV_Position;
    float3 TexCoord : TEXCOORD0;
    float3 HueCoord : TEXCOORD1;
};

/* Terrain/Land */

struct TerrainVSOutput {
    float4 OutputPosition       : SV_Position;
    float4 ScreenPosition       : TEXCOORD0;
    float4 WorldPosition        : TEXCOORD1;
    float3 TexCoord             : TEXCOORD3;
    float3 HueCoord             : TEXCOORD5;
};

struct TerrainPSInput {
    float4 ScreenPosition       : TEXCOORD0;
    float4 WorldPosition        : TEXCOORD1;
    float3 TexCoord             : TEXCOORD3;
    float3 HueCoord             : TEXCOORD5;
};

bool is_zero_vector(float3 v)
{   
    return v.x == 0 && v.y == 0 && v.z == 0;
}

TerrainVSOutput TerrainVSMain(VSInput vin) {
    TerrainVSOutput vout;

    vout.ScreenPosition = mul(vin.Position, WorldViewProj);
    vout.WorldPosition = vin.Position;
    vout.TexCoord = vin.TexCoord;
    vout.HueCoord = vin.HueCoord;

    vout.OutputPosition = vout.ScreenPosition;

    return vout;
}

float4 TerrainPSMain(TerrainPSInput pin) : SV_Target0
{
    float4 color = tex2D(TextureSampler, pin.TexCoord.xy);

    if (color.a == 0)
        discard;
        
    int mode = int(pin.HueCoord.y);
            
    if (mode == HUED || (mode == PARTIAL && color.r == color.g && color.r == color.b))
    {
        float2 hueCoord = float2(color.r, pin.HueCoord.x / HuesPerTexture);
        color.rgb = tex2D(HueSampler, hueCoord).rgb;
    }

    return color;
}

/* Statics */

struct StaticsVSOutput {
    float4 OutputPosition       : SV_Position;
    float4 ScreenPosition       : TEXCOORD0;
    float4 WorldPosition        : TEXCOORD1;
    float3 TexCoord             : TEXCOORD3;
    float3 HueCoord             : TEXCOORD5;
};

struct StaticsPSInput {
    float4 ScreenPosition       : TEXCOORD0;
    float4 WorldPosition        : TEXCOORD1;
    float3 TexCoord             : TEXCOORD3;
    float3 HueCoord             : TEXCOORD5;
};

StaticsVSOutput StaticsVSMain(VSInput vin) {
    StaticsVSOutput vout;

    vout.ScreenPosition = mul(vin.Position, WorldViewProj);
    vout.WorldPosition = vin.Position;
    vout.TexCoord = vin.TexCoord;

    //vout.ScreenPosition.z -= (vin.Position.z / 512.0f) * 0.001f;
    vout.ScreenPosition.z += vin.TexCoord.z;

    vout.OutputPosition = vout.ScreenPosition;
    vout.HueCoord = vin.HueCoord;

    return vout;
}

float4 StaticsPSMain(StaticsPSInput pin) : SV_Target0
{
    float4 color = tex2D(TextureSampler, pin.TexCoord.xy);

    if (color.a == 0)
        discard;
        
    int mode = int(pin.HueCoord.y);
        
    if (mode == HUED || (mode == PARTIAL && color.r == color.g && color.r == color.b))
    {
        float2 hueCoord = float2(color.r, pin.HueCoord.x / HuesPerTexture);
        color.rgb = tex2D(HueSampler, hueCoord).rgb;
    }

    color.a = pin.HueCoord.z;

    return color;
}

/* Selection */

struct SelectionVSOutput {
    float4 OutputPosition       : SV_Position;
    float3 TexCoord             : TEXCOORD0;
    float3 Color                : TEXCOORD1;
};

struct SelectionPSInput {
    float3 TexCoord             : TEXCOORD0;
    float3 Color                : TEXCOORD1;
};

SelectionVSOutput SelectionVSMain(VSInput vin) {
    SelectionVSOutput vout;

    float4 ScreenPosition = mul(vin.Position, WorldViewProj);
    float4 WorldPosition = vin.Position;

    ScreenPosition.z += vin.TexCoord.z;

    vout.OutputPosition = ScreenPosition;
    vout.TexCoord = vin.TexCoord;
    vout.Color = vin.HueCoord;

    return vout;
}

float4 SelectionPSMain(SelectionPSInput pin) : SV_Target0
{
    float4 color = tex2D(TextureSampler, pin.TexCoord.xy);
     if (color.a == 0)
            discard;
    return float4(pin.Color, 1.0);
}

/* VirtualLayer */

struct VirtualLayerVSOutput {
    float4 OutputPosition       : SV_Position;
    float4 WorldPosition        : TEXCOORD0;
};

VirtualLayerVSOutput VirtualLayerVSMain(VSInput vin) {
    VirtualLayerVSOutput vout;
    
    float4 ScreenPosition = mul(vin.Position, WorldViewProj);

    ScreenPosition.z += vin.TexCoord.z;
    
    vout.OutputPosition = ScreenPosition;
    vout.WorldPosition = vin.Position;
    
    return vout;
}

float4 VirtualLayerPSMain(float4 WorldPosition : TEXCOORD0) : SV_Target0
{
    //0.7 worked for me as it's not glitching when moving camera
    if (abs(fmod(WorldPosition.x, TileSize)) < 0.7 || abs(fmod(WorldPosition.y, TileSize)) < 0.7) 
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
        VertexShader = compile vs_2_0 TerrainVSMain();
        PixelShader = compile ps_2_0 TerrainPSMain();
    }
}

Technique Statics {
    Pass
    {
        VertexShader = compile vs_2_0 StaticsVSMain();
        PixelShader = compile ps_2_0 StaticsPSMain();
    }
}

Technique VirtualLayer {
    Pass
    {
        VertexShader = compile vs_2_0 VirtualLayerVSMain();
        PixelShader = compile ps_2_0 VirtualLayerPSMain();
    }
}

Technique Selection {
    Pass
    {
        VertexShader = compile vs_2_0 SelectionVSMain();
        PixelShader = compile ps_2_0 SelectionPSMain();
    }
}