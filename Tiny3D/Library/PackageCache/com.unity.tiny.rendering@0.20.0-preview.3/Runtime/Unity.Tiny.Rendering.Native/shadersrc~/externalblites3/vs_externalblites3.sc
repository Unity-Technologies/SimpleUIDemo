attribute highp vec3 a_position;
attribute highp vec2 a_texcoord0;

uniform highp mat4 u_modelViewProj;
uniform highp vec4 u_color0;

varying highp vec4 v_color0;
varying highp vec2 v_texcoord0; 

void main()
{
	gl_Position = u_modelViewProj * vec4(a_position, 1.0);
	v_color0 = u_color0;
    v_texcoord0 = a_texcoord0;
}
