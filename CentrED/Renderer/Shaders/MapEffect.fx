#define NONE 0
#define HUED 1
#define PARTIAL 2
#define RGB 255

static const float TileSize = 31.11;
static const float3 LIGHT_DIRECTION = float3(0.0f, 1.0f, 1.0f);
static const float Brightlight = 1.5f; //This can be parametrized, but 1.5f is default :)

sampler TextureSampler : register(s0);
sampler HueSampler : register(s1);

//Effect parameters
float4x4 WorldViewProj;
float4 VirtualLayerFillColor;
float4 VirtualLayerBorderColor;
float4 TerrainGridFlatColor;
float4 TerrainGridAngledColor;
float LightLevel;

/* For now, all the techniques use the same vertex definition */
struct VSInput {
    float3 Position : POSITION;
    float3 Texture  : TEXCOORD0;   //uv, screenPos z offset
    float4 Hue      : TEXCOORD1;   //rgb,mode or hueId, unused, alpha, mode
    float3 Normal   : TEXCOORD2;   
};

struct PSInput {
    float4 OutputPosition : SV_Position;
    float3 Texture : TEXCOORD0;
    float4 Hue     : TEXCOORD1;
    float3 Normal  : TEXCOORD2;  
};

bool is_zero_vector(float3 v)
{   
    return v.x == 0 && v.y == 0 && v.z == 0;
}

const static float HUE_ROWS = 1024;
const static float HUE_COLUMNS = 16;
const static float HUE_WIDTH = 32;
const static float HUES_PER_TEXTURE = HUE_ROWS * HUE_COLUMNS;

float3 get_rgb(float gray, float hue)
{
    float halfPixelX = (1.0f / (HUE_COLUMNS * HUE_WIDTH)) * 0.5f;
    float hueColumnWidth = 1.0f / HUE_COLUMNS;
    float hueStart = frac(hue / HUE_COLUMNS);
    
    float xPos = hueStart + gray / HUE_COLUMNS;
    xPos = clamp(xPos, hueStart + halfPixelX, hueStart + hueColumnWidth - halfPixelX);
    float yPos = (hue % HUES_PER_TEXTURE) / (HUES_PER_TEXTURE - 1);
    return tex2D(HueSampler, float2(xPos, yPos)).rgb;
}

//Thanks ClassicUO
float get_light(float3 norm)
{
	float3 light = normalize(LIGHT_DIRECTION);
	float3 normal = normalize(norm);
	float base = (max(dot(normal, light), 0.0f) / 2.0f) + 0.5f;

	// At 45 degrees (the angle the flat tiles are lit at) it must come out
	// to (cos(45) / 2) + 0.5 or 0.85355339...
	return base + ((Brightlight * (base - 0.85355339f)) - (base - 0.85355339f));
}


//Common vertex shader
PSInput TileVSMain(VSInput vin) {
    PSInput vout;

    vout.OutputPosition = mul(float4(vin.Position, 1.0), WorldViewProj);
    vout.OutputPosition.z += vin.Texture.z;
    vout.Texture = vin.Texture;
    vout.Hue = vin.Hue;
    vout.Normal = vin.Normal;

    return vout;
}

PSInput TerrainGridVSMain(VSInput vin) {
    PSInput vout = TileVSMain(vin);

    vout.OutputPosition.z -= 0.01;

    return vout;
}

float4 TerrainPSMain(PSInput pin) : SV_Target0
{
    float4 color = tex2D(TextureSampler, pin.Texture.xy);
    if (color.a == 0)
        discard;
        
    // We use Texture.z to tell shader if it uses TexMap or Art and based on this we apply lighting or not
    // Landtiles in Art come with lighting prebaked into it
    if(pin.Texture.z > 0.0f) 
        color.rgb *= get_light(pin.Normal);
    
    if((int)pin.Hue.a == RGB)
        color.rgb += pin.Hue.rgb;
        
    color.rgb *= LightLevel;
    
    return color;
}

float4 TerrainGridPSMain(PSInput pin) : SV_Target0
{
    if(pin.Texture.z > 0.0f) 
        return TerrainGridAngledColor;
    else
        return TerrainGridFlatColor;
}

float4 StaticsPSMain(PSInput pin) : SV_Target0
{
    float4 color = tex2D(TextureSampler, pin.Texture.xy);
    if (color.a == 0)
        discard;
        
    int mode = int(pin.Hue.a);
    
    if (mode == HUED || (mode == PARTIAL && color.r == color.g && color.r == color.b))
    {
        color.rgb = get_rgb(color.r, pin.Hue.x);
    }
    else if (mode == RGB)
    {
        color.rgb += pin.Hue.rgb;
    }

    if (mode != RGB)
    {
        color.a = pin.Hue.z;
    }

    color.rgb *= LightLevel;
  
    return color;
}

float4 SelectionPSMain(PSInput pin) : SV_Target0
{
    float4 color = tex2D(TextureSampler, pin.Texture.xy);
     if (color.a == 0)
            discard;
    return pin.Hue;
}

PSInput VirtualLayerVSMain(VSInput vin) {
    PSInput vout;
    
    vout.OutputPosition = mul(float4(vin.Position, 1.0), WorldViewProj);
    vout.Texture = vin.Position;
    vout.Hue = vin.Hue;
    vout.Normal = vin.Normal;
    
    return vout;
}

float4 VirtualLayerPSMain(PSInput pin) : SV_Target0
{
    //0.7 worked for me as it's not glitching when moving camera
    if (abs(fmod(pin.Texture.x, TileSize)) < 0.7 || abs(fmod(pin.Texture.y, TileSize)) < 0.7) 
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

Technique TerrainGrid
{
    Pass
    {
        FillMode = Wireframe;
        VertexShader = compile vs_2_0 TerrainGridVSMain();
        PixelShader = compile ps_2_0 TerrainGridPSMain();
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