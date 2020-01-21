$input v_color0, v_texcoord0

#include "../common/common.sh"

SAMPLER2D(s_texColor,  0);

void main()
{
	vec2 texcoord0 = vec2(v_texcoord0.x, 1.0f - v_texcoord0.y);
    vec4 albedo = texture2D(s_texColor, texcoord0) * v_color0;    
	gl_FragColor = albedo * albedo.wwww;    // for Alpha
}
