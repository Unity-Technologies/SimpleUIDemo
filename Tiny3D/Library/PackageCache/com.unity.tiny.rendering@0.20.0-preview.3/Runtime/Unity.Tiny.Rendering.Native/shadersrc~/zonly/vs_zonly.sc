$input a_position
$output v_dummy;

#include "../common/common.sh"

void main()
{
    vec4 p = mul(u_modelViewProj, vec4(a_position, 1.0) );
	gl_Position = p;
}
