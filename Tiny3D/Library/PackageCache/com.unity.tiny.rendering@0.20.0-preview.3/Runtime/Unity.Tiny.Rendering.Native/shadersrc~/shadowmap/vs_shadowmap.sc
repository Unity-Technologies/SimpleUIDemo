$input a_position
$output v_dummy;

#include "../common/common.sh"

uniform vec4 u_bias; 

void main()
{
    vec4 p = mul(u_modelView, vec4(a_position, 1.0) );
    p.z += u_bias.x; // light space constant bias
    p = mul (u_proj, p);
    p.z += u_bias.y * p.w; // projected bias
    gl_Position = p;
}
