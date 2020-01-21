varying highp vec4 v_color0;
varying highp vec2 v_texcoord0;

uniform sampler2D s_texColor;

void main()
{
	gl_FragColor = texture2D(s_texColor, v_texcoord0) * v_color0;
}
