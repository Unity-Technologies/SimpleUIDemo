$input v_color0, v_texcoord0

/*
 * Copyright 2011-2015 Branimir Karadzic. All rights reserved.
 * License: http://www.opensource.org/licenses/BSD-2-Clause
 */

#include "../common/common.sh"

float pow4(float x) {
    float x2 = x*x;
    return x2*x2;
}

void main()
{
    float c = 1.0 - pow4(v_texcoord0.y);
	gl_FragColor = c * v_color0;
}
