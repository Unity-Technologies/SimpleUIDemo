$input v_color0, v_texcoord0

#include "../common/common.sh"

SAMPLER2D(s_texColor,  0);

uniform vec4 u_colormul;
uniform vec4 u_coloradd;
uniform vec4 u_decodeSRGB_encodeSRGB_reinhard_premultiply; 

void main()
{
    vec4 c = texture2D(s_texColor, v_texcoord0);

    if (u_decodeSRGB_encodeSRGB_reinhard_premultiply.x != 0.0) {
        c = toLinearAccurate(c);
    }

    c = c * v_color0;
    c = c * u_colormul + u_coloradd;

    if (u_decodeSRGB_encodeSRGB_reinhard_premultiply.z != 0.0) {
        c.xyz = c.xyz / (c.xyz + vec3_splat(u_decodeSRGB_encodeSRGB_reinhard_premultiply.z));
    }

    if (u_decodeSRGB_encodeSRGB_reinhard_premultiply.y != 0.0) {
        c = toGammaAccurate(c);
    }

    if ( u_decodeSRGB_encodeSRGB_reinhard_premultiply.w != 0.0 ) {
        c.xyz = c.xyz * c.w;
    }

    gl_FragColor = c;
}
