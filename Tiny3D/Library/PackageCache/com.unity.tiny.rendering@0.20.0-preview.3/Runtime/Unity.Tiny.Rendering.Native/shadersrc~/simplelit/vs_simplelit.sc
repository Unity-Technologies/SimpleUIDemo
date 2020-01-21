$input a_position, a_texcoord0, a_normal, a_tangent, a_bitangent, a_color0, a_texcoord1, a_color1
$output v_texcoord0_metal_smoothness, v_normal, v_tangent, v_bitangent, v_albedo_opacity, v_pos, v_light0pos, v_light1pos

#include "../common/common.sh"

uniform vec4 u_albedo_opacity;
uniform vec4 u_metal_smoothness;
uniform mat4 u_wl_light0;
uniform mat4 u_wl_light1;
uniform vec4 u_texmad;
uniform mat3 u_modelInverseTranspose;

void main()
{
    gl_Position = mul(u_modelViewProj, vec4(a_position, 1.0));
    vec2 tt = transformTex(a_texcoord0, u_texmad);
    vec2 ms = a_texcoord1 * u_metal_smoothness.xy;
    v_texcoord0_metal_smoothness = vec4(tt.x, tt.y, ms.x, ms.y);

    mat3 view3 = mat3(u_view[0].xyz, u_view[1].xyz, u_view[2].xyz);
    mat3 mvit = mul(view3, u_modelInverseTranspose);

    v_normal = mul(mvit, a_normal); 
    v_albedo_opacity = a_color0 * u_albedo_opacity;
    v_pos = mul(u_modelView, vec4(a_position, 1.0)).xyz;

    vec4 wspos = mul(u_model[0], vec4(a_position, 1.0));  // model -> world
    v_light0pos = mul(u_wl_light0, wspos);                // world -> light0
    v_light1pos = mul(u_wl_light1, wspos);                // world -> light1

    v_tangent = mul(mvit, a_tangent);
    v_bitangent = mul(mvit, a_bitangent);
}
