$input v_texcoord0_metal_smoothness, v_normal, v_tangent, v_bitangent, v_albedo_opacity, v_pos, v_light0pos, v_light1pos

#include "../common/common.sh"

#define EMULATEPCF

SAMPLER2D(s_texAlbedo, 0);
SAMPLER2D(s_texMetal, 1);
SAMPLER2D(s_texNormal, 2);
SAMPLER2D(s_texSmoothness, 3);
SAMPLER2D(s_texEmissive, 4);
SAMPLER2D(s_texOpacity, 5);

#define PI 3.14159265

uniform vec4 u_ambient;
uniform vec4 u_emissive_normalz;

uniform vec4 u_numlights; // x=simple y=mapped

// unmapped point or directional lights
uniform vec4 u_simplelight_posordir[8];
uniform vec4 u_simplelight_color_ivr[8];

// mapped light0
uniform vec4 u_light_color_ivr0;
uniform vec4 u_light_pos0;
uniform vec4 u_light_mask0;
SAMPLER2DSHADOW(s_texShadow0, 6);

// mapped light1 
uniform vec4 u_light_color_ivr1;
uniform vec4 u_light_pos1;
uniform vec4 u_light_mask1;
SAMPLER2DSHADOW(s_texShadow1, 7);

uniform vec4 u_texShadow01sis; // x=s_texShadow0.size, y=1/s_texShadow0.size, z=s_texShadow1.size, w=1/s_texShadow1.size

// debug only
uniform vec4 u_outputdebugselect;

// unity std brdf
float OneMinusReflectivityFromMetallic(float metallic) {
    float oneMinusDielectricSpec = 1.0 - 0.04;
    return oneMinusDielectricSpec - metallic * oneMinusDielectricSpec;
}

vec3 SafeNormalize(vec3 inVec)
{
    float dp3 = max(0.001, dot(inVec, inVec));
    return inVec / sqrt(dp3); // no rsqrt
}

float PerceptualRoughnessToRoughness(float perceptualRoughness) {
    return perceptualRoughness * perceptualRoughness;
}

float Pow5(float x) {
    return x * x * x*x * x;
}

vec3 FresnelTerm(vec3 F0, float cosA) {
    float t = Pow5(1.0 - cosA);   // ala Schlick interpoliation
    return F0 + (1.0 - F0) * t;
}

vec3 FresnelLerp(vec3 F0, vec3 F90, float cosA) {
    float t = Pow5(1.0 - cosA);   // ala Schlick interpoliation
    return mix(F0, F90, t);
}

float DisneyDiffuse(float NdotV, float NdotL, float LdotH, float perceptualRoughness) {
    float fd90 = 0.5 + 2.0 * LdotH * LdotH * perceptualRoughness;
    float lightScatter = (1.0 + (fd90 - 1.0) * Pow5(1.0 - NdotL));
    float viewScatter = (1.0 + (fd90 - 1.0) * Pow5(1.0 - NdotV));
    return lightScatter * viewScatter;
}

float SmithJointGGXVisibilityTerm(float NdotL, float NdotV, float roughness) {
    // Approximation of the above formulation (simplify the sqrt, not mathematically correct but close enough)
    float a = roughness;
    float lambdaV = NdotL * (NdotV * (1.0 - a) + a);
    float lambdaL = NdotV * (NdotL * (1.0 - a) + a);
    return 0.5 / (lambdaV + lambdaL + 1e-5);
}

float GGXTerm(float NdotH, float roughness) {
    float a2 = roughness * roughness;
    float d = (NdotH * a2 - NdotH) * NdotH + 1.0; 
    return (1.0 / PI) * a2 / (d * d + 1e-7); // This function is not intended to be running on Mobile, therefore epsilon is smaller than what can be represented by half
}

// Unity normal maps encoding: DXT5nm (1, y, 1, x) or BC5 (x, y, 0, 1)
// Note neutral texture like "bump" is (0, 0, 1, 1) to work with both plain RGB normal and DXT5nm/BC5
vec3 UnpackNormalmapRGorAG(vec4 packednormal)
{
	//Move w to x (RG -> AG)
	packednormal.x *= packednormal.w;

	//Unpack: (0..1) -> (-1, 1)
    vec3 normal;
    normal.xy = packednormal.xy * 2.0 - 1.0;
	//Compute z
    normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
    return normal;
}

float LightMask(vec3 ndcpos, vec4 params)
{
    vec2 s = params.xy * ndcpos.xy;
    return min ( max ( params.z - dot(s, s), params.w ), 1.0 );
}

void AddOneLight(vec3 lightdir, vec3 viewdir, vec3 normal, float nv, float perceptualRoughness, float roughness, vec3 lightcolor, vec3 spec, inout vec3 diffsum, inout vec3 specsum )
{
    vec3 floatdir = SafeNormalize(vec3(lightdir) + viewdir);
    float nh = saturate(dot(normal, floatdir));
    float nl = saturate(dot(normal, lightdir));

    float lv = saturate(dot(lightdir, viewdir));
    float lh = saturate(dot(lightdir, floatdir));

    float diffuseTerm = DisneyDiffuse(nv, nl, lh, perceptualRoughness) * nl;
    float V = SmithJointGGXVisibilityTerm(nl, nv, roughness);
    float D = GGXTerm(nh, roughness);
    float specularTerm = V * D;
    specularTerm = max(0.0, specularTerm * nl);

    // To provide true Lambert lighting, we need to be able to kill specular completely.
    // specularTerm *= any(spec) ? 1.0 : 0.0;

    diffsum += lightcolor * diffuseTerm; 
    specsum += specularTerm * lightcolor * FresnelTerm(spec, lh);
}

float bilinearMix(vec4 s, vec2 coord, float texSize) {
    vec2 fr = fract(coord * texSize);
    vec2 s2 = mix(s.xy, s.zw, fr.x);
    return mix(s2.x, s2.y, fr.y);
}

// this is pretty crazy that we have to write it like this,
// but shaderc has no goo type for shadow samplers, so we can not
// pass them as function arguments 

#define SAMPLEFOURSHADOW(_sampler, _coord, _d)\
    vec4 (\
        shadow2D(_sampler, _coord),\
        shadow2D(_sampler, _coord + vec3(0.0, _d, 0.0)),\
        shadow2D(_sampler, _coord + vec3(_d, 0.0, 0.0)),\
        shadow2D(_sampler, _coord + vec3(_d, _d, 0.0)))

#ifdef EMULATEPCF
   #define shadow2DPCF(_sampler, _coord, _texsize, _invtexsize) bilinearMix(SAMPLEFOURSHADOW(_sampler,_coord,_invtexsize), _coord.xy, _texsize)
#else
   #define shadow2DPCF(_sampler, _coord, _texsize, _invtexsize) shadow2D(_sampler, _coord)
#endif

void main()
{
    vec3 albedo = texture2D(s_texAlbedo, v_texcoord0_metal_smoothness.xy).xyz * v_albedo_opacity.xyz;
    float opacity = texture2D(s_texOpacity, v_texcoord0_metal_smoothness.xy).w * v_albedo_opacity.w;
    float metal = texture2D(s_texMetal, v_texcoord0_metal_smoothness.xy).x * v_texcoord0_metal_smoothness.z;
    float smoothness = texture2D(s_texSmoothness, v_texcoord0_metal_smoothness.xy).w * v_texcoord0_metal_smoothness.w;

    //Unpack unity normal maps
    vec3 texNormal = UnpackNormalmapRGorAG(texture2D(s_texNormal, v_texcoord0_metal_smoothness.xy).xyzw).xyz;
    texNormal.z *= u_emissive_normalz.w; 

    // tangent to view space 
    vec3 normal;
    normal.x = dot(vec3(v_tangent.x, v_bitangent.x, v_normal.x), texNormal);
    normal.y = dot(vec3(v_tangent.y, v_bitangent.y, v_normal.y), texNormal);
    normal.z = dot(vec3(v_tangent.z, v_bitangent.z, v_normal.z), texNormal);

    normal = normalize(normal);

    // metal to specular 
    vec3 spec = mix(vec3(0.04, 0.04, 0.04), albedo, metal);
    float oneMinusReflectivity = OneMinusReflectivityFromMetallic(metal);
    albedo =  albedo * oneMinusReflectivity;
     
    vec3 viewdir = -normalize(v_pos); // view space 

    vec3 diffsum = u_ambient.xyz;; 
    vec3 specsum = vec3(0.0, 0.0, 0.0);

    // shade
    float perceptualRoughness = 1.0 - smoothness; 
    float nv = abs(dot(normal, viewdir));
    float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    roughness = max(roughness, 0.002);

    // float surfaceReduction = 1.0 / (roughness*roughness + 1.0);
    // float grazingTerm = saturate((1.0 - perceptualRoughness) + (1.0 - oneMinusReflectivity));
    // vec3 diffsum = gi.diffuse;
    // vec3 specsum = surfaceReduction * gi.specular * FresnelLerp(specColor, grazingTerm, nv);
    
    // mapped 0
    if ( u_numlights.y > 0.0 ) {
        if ( max ( max( abs(v_light0pos).x, abs(v_light0pos).y) , abs(v_light0pos).z) < v_light0pos.w ) {
            vec3 lightprojp = v_light0pos.xyz / v_light0pos.w;
            vec3 lightproj = lightprojp * 0.5 + vec3_splat(0.5);
            float shadow = shadow2DPCF(s_texShadow0, lightproj, u_texShadow01sis.x, u_texShadow01sis.y);
            if ( shadow > 0.001 ) {
                vec3 lightc = u_light_color_ivr0.xyz;
                lightc *= LightMask(lightprojp, u_light_mask0);
                lightc *= shadow;
                vec3 lightdir = u_light_pos0.xyz - v_pos * u_light_pos0.w;
                float distsqr = dot(lightdir,lightdir);
                lightc *= max(1.0 - distsqr * u_light_color_ivr0.w, 0.0);
                AddOneLight(normalize(lightdir), viewdir, normal, nv, perceptualRoughness, roughness, lightc, spec, diffsum, specsum );
            }
        }
        // mapped 1
        if ( u_numlights.y > 1.0 ) {
            if ( max ( max( abs(v_light1pos).x, abs(v_light1pos).y) , abs(v_light1pos).z) < v_light1pos.w ) {
                vec3 lightprojp = v_light1pos.xyz / v_light1pos.w;
                vec3 lightproj = lightprojp * 0.5 + vec3_splat(0.5);
                float shadow = shadow2DPCF(s_texShadow1, lightproj, u_texShadow01sis.z, u_texShadow01sis.w);
                if ( shadow > 0.001 ) {
                    vec3 lightc = u_light_color_ivr1.xyz;
                    lightc *= LightMask(lightprojp, u_light_mask1);
                    lightc *= shadow;
                    vec3 lightdir = u_light_pos1.xyz - v_pos * u_light_pos1.w;
                    float distsqr = dot(lightdir,lightdir);
                    lightc *= max(1.0 - distsqr * u_light_color_ivr1.w, 0.0);
                    AddOneLight(normalize(lightdir), viewdir, normal, nv, perceptualRoughness, roughness, lightc, spec, diffsum, specsum );
                }
            }
        }
    }

    // directional or point lights
    int nl = int(u_numlights.x);
    for ( int i=0; i<8; i++ ) {
        // this is a really stupid hack to get around the combined limitiations of shaderc and webgl:
        // - webgl can not do non constant loops 
        // - shaderc transforms loops with an "if (i>=nl) break" into a while(true) loop, which does not work in webgl 
        if ( i<nl ) { 
            vec4 posordir = u_simplelight_posordir[i];
            vec4 color_ivr = u_simplelight_color_ivr[i];
            vec3 lightdir = posordir.xyz - v_pos * posordir.w; // w = 0 for directional lights
            float atten = max(1.0 - dot(lightdir,lightdir) * color_ivr.w, 0.0); // ivr = 0 for directional lights
            if ( atten > 0.001 )
                AddOneLight(normalize(lightdir), viewdir, normal, nv, perceptualRoughness, roughness, atten * color_ivr.xyz, spec, diffsum, specsum );
        }
    }

    // finalize
    vec3 c = albedo * diffsum * opacity + specsum + u_emissive_normalz.xyz;

    // debug only
    c = mix ( c, diffsum, u_outputdebugselect.x);
    c = mix ( c, normal, u_outputdebugselect.y);
    c = mix ( c, specsum, u_outputdebugselect.z);
    c = mix ( c, v_normal.xyz, u_outputdebugselect.w);

    gl_FragColor = vec4(c,opacity);
    
    // gl_FragColor = vec4(normal + c * 0.00001,opacity);
}
