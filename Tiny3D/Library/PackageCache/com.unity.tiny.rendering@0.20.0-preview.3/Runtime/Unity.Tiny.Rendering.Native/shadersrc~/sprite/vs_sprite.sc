$input a_position, a_texcoord0
$output v_color0, v_texcoord0

#include "../common/common.sh"

uniform vec4 u_tint0;

void main()
{
	gl_Position = mul(u_modelViewProj, vec4(a_position, 1.0) );
	v_color0 = u_tint0;
    v_texcoord0 = a_texcoord0;
}
