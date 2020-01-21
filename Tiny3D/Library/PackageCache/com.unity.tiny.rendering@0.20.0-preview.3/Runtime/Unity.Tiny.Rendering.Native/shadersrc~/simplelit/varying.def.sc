vec4 v_texcoord0_metal_smoothness           : TEXCOORD0  = vec4(0.0, 0.0, 0.0, 0.5);
vec3 v_normal                               : NORMAL     = vec3(0.0, 0.0, 1.0);
vec3 v_tangent                              : TANGENT    = vec3(1.0, 0.0, 0.0);
vec3 v_bitangent                            : BITANGENT  = vec3(0.0, 1.0, 0.0);
vec4 v_albedo_opacity                       : COLOR0     = vec4(1.0, 1.0, 1.0, 1.0);
vec3 v_pos                                  : TEXCOORD1  = vec3(0.0, 0.0, 0.0);
vec4 v_light0pos                            : TEXCOORD4  = vec4(0.5, 0.5, -1.0, 1.0);
vec4 v_light1pos                            : TEXCOORD5  = vec4(0.5, 0.5, -1.0, 1.0);

vec3 a_position   : POSITION;
vec2 a_texcoord0  : TEXCOORD0;
vec3 a_normal     : NORMAL;
vec3 a_tangent    : TANGENT;
vec3 a_bitangent  : BITANGENT;
vec4 a_color0     : COLOR0;
vec2 a_texcoord1  : TEXCOORD1;

