#define NONE 0
#define HUED 1
#define PARTIAL 2

static const float HuesPerTexture = 3000;
static const float TileSize = 31.11;

sampler TextureSampler : register(s0);
sampler ShadowSampler : register(s1);
sampler HueSampler : register(s2);

cbuffer ProjectionMatrix : register(b0) {
    float4x4 WorldViewProj;
    float4x4 LightWorldViewProj;
};

cbuffer LightParameters : register(b1) {
    float4 AmbientLightColor;

    float3 DirectionalLightPosition;
    float3 DirectionalLightDirection;
    float3 DirectionalLightDiffuseColor;
    float3 DirectionalLightSpecularColor;
};

/* For now, all the techniques use the same vertex definition */
struct VSInput {
    float4 Position : SV_Position;
    float3 Normal   : NORMAL;
    float3 TexCoord : TEXCOORD0;
    float3 HueCoord : TEXCOORD1;
};

/* Terrain/Land */

struct TerrainVSOutput {
    float4 OutputPosition       : SV_Position;
    float4 ScreenPosition       : TEXCOORD0;
    float4 WorldPosition        : TEXCOORD1;
    float4 LightViewPosition    : TEXCOORD2;
    float3 TexCoord             : TEXCOORD3;
    float3 Normal               : TEXCOORD4;
    float3 HueCoord             : TEXCOORD5;
};

struct TerrainPSInput {
    float4 ScreenPosition       : TEXCOORD0;
    float4 WorldPosition        : TEXCOORD1;
    float4 LightViewPosition    : TEXCOORD2;
    float3 TexCoord             : TEXCOORD3;
    float3 Normal               : TEXCOORD4;
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
    vout.LightViewPosition = mul(vin.Position, LightWorldViewProj);
    vout.Normal = vin.Normal;
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

    float2 LightViewTexCoords;
    LightViewTexCoords.x = (pin.LightViewPosition.x / pin.LightViewPosition.w) / 2.0f + 0.5f;
    LightViewTexCoords.y = (-pin.LightViewPosition.y / pin.LightViewPosition.w) / 2.0f + 0.5f; //zero y in a texture is the top, so reverse coordinates

    // get depth from shadow map red channel
    float lightViewDepth = tex2D(ShadowSampler, LightViewTexCoords).r;

    // compare shadow-map depth to the actual depth
    // subtracting a small value helps avoid floating point equality errors (depth bias)
    // when the distances are equal
    if(!is_zero_vector(DirectionalLightDiffuseColor) || !is_zero_vector(DirectionalLightSpecularColor)){
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
    }

    return color;
}

/* Statics */

struct StaticsVSOutput {
    float4 OutputPosition       : SV_Position;
    float4 ScreenPosition       : TEXCOORD0;
    float4 WorldPosition        : TEXCOORD1;
    float4 LightViewPosition    : TEXCOORD2;
    float3 TexCoord             : TEXCOORD3;
    float3 Normal               : TEXCOORD4;
    float3 HueCoord             : TEXCOORD5;
};

struct StaticsPSInput {
    float4 ScreenPosition       : TEXCOORD0;
    float4 WorldPosition        : TEXCOORD1;
    float4 LightViewPosition    : TEXCOORD2;
    float3 TexCoord             : TEXCOORD3;
    float3 Normal               : TEXCOORD4;
    float3 HueCoord             : TEXCOORD5;
};

StaticsVSOutput StaticsVSMain(VSInput vin) {
    StaticsVSOutput vout;

    vout.ScreenPosition = mul(vin.Position, WorldViewProj);
    vout.WorldPosition = vin.Position;
    vout.LightViewPosition = mul(vin.Position, LightWorldViewProj);
    vout.Normal = vin.Normal;
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

    float2 LightViewTexCoords;
    LightViewTexCoords.x = (pin.LightViewPosition.x / pin.LightViewPosition.w) / 2.0f + 0.5f;
    LightViewTexCoords.y = (-pin.LightViewPosition.y / pin.LightViewPosition.w) / 2.0f + 0.5f; //zero y in a texture is the top, so reverse coordinates

    // get depth from shadow map red channel
    float lightViewDepth = tex2D(ShadowSampler, LightViewTexCoords).r;

    // compare shadow-map depth to the actual depth
    // subtracting a small value helps avoid floating point equality errors (depth bias)
    // when the distances are equal
    if(!is_zero_vector(DirectionalLightDiffuseColor) || !is_zero_vector(DirectionalLightSpecularColor)){
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
    }
    color.a = pin.HueCoord.z;

    return color;
}

/* ShadowMap */

struct ShadowMapVSOutput {
    float4 OutputPosition       : SV_Position;
    float4 ScreenPosition       : TEXCOORD0;
    float3 TexCoord             : TEXCOORD1;
};

struct ShadowMapPSInput {
    float4 ScreenPosition       : TEXCOORD0;
    float3 TexCoord             : TEXCOORD1;
};

ShadowMapVSOutput ShadowMapVSMain(VSInput vin) {
    ShadowMapVSOutput vout;

    vout.ScreenPosition = mul(vin.Position, WorldViewProj);
    vout.TexCoord = vin.TexCoord;

    vout.ScreenPosition.z += vin.TexCoord.z;

    vout.OutputPosition = vout.ScreenPosition;

    return vout;
}

float4 ShadowMapPSMain(ShadowMapPSInput pin) : SV_Target0
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
            return float4(1.0, 1.0, 1.0, 1.0);
    } 
    else 
    {
            return float4(0.2, 0.2, 0.2, 0.1);
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

Technique ShadowMap {
    Pass
    {
        VertexShader = compile vs_2_0 ShadowMapVSMain();
        PixelShader = compile ps_2_0 ShadowMapPSMain();
    }
}

Technique Selection {
    Pass
    {
        VertexShader = compile vs_2_0 SelectionVSMain();
        PixelShader = compile ps_2_0 SelectionPSMain();
    }
}