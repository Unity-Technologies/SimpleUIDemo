/*
 * Copyright 2011-2019 Branimir Karadzic. All rights reserved.
 * License: https://github.com/bkaradzic/bx#license-bsd-2-clause
 */

#ifndef BX_EASING_H_HEADER_GUARD
#define BX_EASING_H_HEADER_GUARD

#include "math.h"

namespace bx
{
	///
	struct Easing
	{
		enum Enum
		{
			Linear,
			Step,
			SmoothStep,
			InQuad,
			OutQuad,
			InOutQuad,
			OutInQuad,
			InCubic,
			OutCubic,
			InOutCubic,
			OutInCubic,
			InQuart,
			OutQuart,
			InOutQuart,
			OutInQuart,
			InQuint,
			OutQuint,
			InOutQuint,
			OutInQuint,
			InSine,
			OutSine,
			InOutSine,
			OutInSine,
			InExpo,
			OutExpo,
			InOutExpo,
			OutInExpo,
			InCirc,
			OutCirc,
			InOutCirc,
			OutInCirc,
			InElastic,
			OutElastic,
			InOutElastic,
			OutInElastic,
			InBack,
			OutBack,
			InOutBack,
			OutInBack,
			InBounce,
			OutBounce,
			InOutBounce,
			OutInBounce,

			Count
		};
	};

	///
	typedef float (*EaseFn)(float _t);

	///
	EaseFn getEaseFunc(Easing::Enum _enum);

	/// Linear.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                                         *******
	///      |                                                   *******
	///      |                                            ********
	///      |                                      *******
	///      |                                *******
	///      |                         ********
	///      |                   *******
	///      |            ********
	///      |      *******
	///      +*******--------------------------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeLinear(float _t);

	/// Step.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |                                ********************************
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |
	///      +********************************-------------------------------->
	///      |
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeStep(float _t);

	/// Smooth step.
	///
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                                   *************
	///      |                                             *******
	///      |                                        ******
	///      |                                    *****
	///      |                                *****
	///      |                           *****
	///      |                       *****
	///      |                  ******
	///      |            *******
	///      +*************--------------------------------------------------->
	///      |
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeSmoothStep(float _t);

	/// Quad.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                                            ****
	///      |                                                         ****
	///      |                                                     *****
	///      |                                                 *****
	///      |                                             *****
	///      |                                        ******
	///      |                                   ******
	///      |                            ********
	///      |                    *********
	///      +*********************------------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeInQuad(float _t);

	/// Out quad.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                           *********************
	///      |                                   *********
	///      |                             *******
	///      |                       ******
	///      |                  ******
	///      |              *****
	///      |          *****
	///      |      *****
	///      |   ****
	///      +****------------------------------------------------------------>
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeOutQuad(float _t);

	/// In out quad.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                                 ***************
	///      |                                           *******
	///      |                                       *****
	///      |                                   *****
	///      |                                ****
	///      |                            *****
	///      |                        *****
	///      |                    *****
	///      |              *******
	///      +***************------------------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeInOutQuad(float _t);

	/// Out in quad.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                                            ****
	///      |                                                        *****
	///      |                                                    *****
	///      |                                              *******
	///      |                                ***************
	///      |                 ****************
	///      |           *******
	///      |       *****
	///      |   *****
	///      +****------------------------------------------------------------>
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeOutInQuad(float _t);

	/// In cubic.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                                             ***
	///      |                                                           ***
	///      |                                                        ****
	///      |                                                      ***
	///      |                                                  ****
	///      |                                               ****
	///      |                                          ******
	///      |                                     ******
	///      |                             *********
	///      +******************************---------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeInCubic(float _t);

	/// Out cubic.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |                                                               *
	///      |                                  ******************************
	///      |                          *********
	///      |                     ******
	///      |                ******
	///      |             ****
	///      |          ****
	///      |       ****
	///      |    ****
	///      |  ***
	///      +***------------------------------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeOutCubic(float _t);

	/// In out cubic.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |                                                               *
	///      |                                             *******************
	///      |                                        ******
	///      |                                     ****
	///      |                                  ****
	///      |                                ***
	///      |                             ****
	///      |                           ***
	///      |                       ****
	///      |                  ******
	///      +*******************--------------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeInOutCubic(float _t);

	/// Out in cubic.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                                             ***
	///      |                                                           ***
	///      |                                                       ****
	///      |                                                  ******
	///      |                                *******************
	///      |             ********************
	///      |        ******
	///      |     ****
	///      |  ****
	///      +***------------------------------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeOutInCubic(float _t);

	/// In quart.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                                              **
	///      |                                                            ***
	///      |                                                          ***
	///      |                                                        ***
	///      |                                                     ****
	///      |                                                  ****
	///      |                                               ****
	///      |                                          ******
	///      |                                    *******
	///      +************************************---------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeInQuart(float _t);

	/// Out quart.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |                                                               *
	///      |                            ************************************
	///      |                     ********
	///      |                ******
	///      |             ****
	///      |          ****
	///      |       ****
	///      |     ***
	///      |   ***
	///      | ***
	///      +**-------------------------------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeOutQuart(float _t);

	/// In out quart.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |                                                               *
	///      |                                          **********************
	///      |                                      *****
	///      |                                   ****
	///      |                                 ***
	///      |                                **
	///      |                              ***
	///      |                            ***
	///      |                         ****
	///      |                     *****
	///      +**********************------------------------------------------>
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeInOutQuart(float _t);

	/// Out in quart.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                                              **
	///      |                                                            ***
	///      |                                                         ****
	///      |                                                     *****
	///      |                               ***********************
	///      |          ***********************
	///      |      *****
	///      |   ****
	///      | ***
	///      +**-------------------------------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeOutInQuart(float _t);

	/// In quint.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                                              **
	///      |                                                             **
	///      |                                                           ***
	///      |                                                         ***
	///      |                                                       ***
	///      |                                                     ***
	///      |                                                  ****
	///      |                                              *****
	///      |                                        *******
	///      +*****************************************----------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeInQuint(float _t);

	/// Out quint.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |                                                              **
	///      |                       *****************************************
	///      |                 *******
	///      |             *****
	///      |          ****
	///      |        ***
	///      |      ***
	///      |    ***
	///      |  ***
	///      | **
	///      +**-------------------------------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeOutQuint(float _t);

	/// In out quint.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |                                                              **
	///      |                                        ************************
	///      |                                     ****
	///      |                                   ***
	///      |                                 ***
	///      |                                **
	///      |                              ***
	///      |                            ***
	///      |                          ***
	///      |                       ****
	///      +************************---------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeInOutQuint(float _t);

	/// Out in quint.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                                              **
	///      |                                                            ***
	///      |                                                          ***
	///      |                                                       ****
	///      |                               *************************
	///      |        **************************
	///      |     ****
	///      |   ***
	///      | ***
	///      +**-------------------------------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeOutInQuint(float _t);

	/// In sine.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                                            ****
	///      |                                                       *****
	///      |                                                   *****
	///      |                                               *****
	///      |                                          ******
	///      |                                     ******
	///      |                                ******
	///      |                          *******
	///      |                  *********
	///      +*******************--------------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeInSine(float _t);

	/// Out sine.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                             *******************
	///      |                                     *********
	///      |                               *******
	///      |                          ******
	///      |                     ******
	///      |                ******
	///      |            *****
	///      |        *****
	///      |    *****
	///      +*****----------------------------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeOutSine(float _t);

	/// In out sine.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                                  **************
	///      |                                             ******
	///      |                                        ******
	///      |                                    *****
	///      |                                *****
	///      |                           ******
	///      |                       *****
	///      |                  ******
	///      |             ******
	///      +**************-------------------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeInOutSine(float _t);

	/// Out in sine.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                                           *****
	///      |                                                       *****
	///      |                                                  ******
	///      |                                             ******
	///      |                                **************
	///      |                  ***************
	///      |             ******
	///      |        ******
	///      |    *****
	///      +*****----------------------------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeOutInSine(float _t);

	/// In exponential.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                                               *
	///      |                                                              **
	///      |                                                            **
	///      |                                                           **
	///      |                                                         ***
	///      |                                                       ***
	///      |                                                     ***
	///      |                                                 ****
	///      |                                          ********
	///      +*******************************************--------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeInExpo(float _t);

	/// Out exponential.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |                                                               *
	///      |                     *******************************************
	///      |              ********
	///      |           ****
	///      |        ****
	///      |      ***
	///      |    ***
	///      |   **
	///      |  **
	///      | **
	///      +*--------------------------------------------------------------->
	///      |
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeOutExpo(float _t);

	/// In out exponential.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |                                                               *
	///      |                                       *************************
	///      |                                    ****
	///      |                                  ***
	///      |                                 **
	///      |                                **
	///      |                               *
	///      |                             **
	///      |                           ***
	///      |                        ****
	///      +*************************--------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeInOutExpo(float _t);

	/// Out in exponential.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                                               *
	///      |                                                             **
	///      |                                                           ***
	///      |                                                        ****
	///      |                               **************************
	///      |       **************************
	///      |    ****
	///      |  ***
	///      | **
	///      +**-------------------------------------------------------------->
	///      |
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeOutInExpo(float _t);

	/// In circle.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                                               *
	///      |                                                              **
	///      |                                                             **
	///      |                                                          ****
	///      |                                                       ****
	///      |                                                   *****
	///      |                                             *******
	///      |                                      ********
	///      |                           ************
	///      +****************************------------------------------------>
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeInCirc(float _t);

	/// Out circle.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                    ****************************
	///      |                         ************
	///      |                  ********
	///      |            *******
	///      |        *****
	///      |     ****
	///      |   ***
	///      | **
	///      |**
	///      +*--------------------------------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeOutCirc(float _t);

	/// In out circle.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                            ********************
	///      |                                      *******
	///      |                                  *****
	///      |                                ***
	///      |                                *
	///      |                               **
	///      |                             ***
	///      |                         *****
	///      |                   *******
	///      +********************-------------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeInOutCirc(float _t);

	/// Out in circle.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                                               *
	///      |                                                             ***
	///      |                                                         *****
	///      |                                                   *******
	///      |                                ********************
	///      |            *********************
	///      |      *******
	///      |  *****
	///      |***
	///      +*--------------------------------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeOutInCirc(float _t);

	/// Out elastic.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                                               *
	///      |                                                              **
	///      |                                                              *
	///      |                                                             **
	///      |                                                             *
	///      |                                                             *
	///      |                                                            *
	///      |                                                            *
	///      |                                           *****           **
	///      +-***********--------***********---------****---***---------*---->
	///      |**         **********         ***********        **       **
	///      |                                                   **     *
	///      |                                                    **   *
	///      |                                                     *****
	///      |
	///
	BX_CONST_FUNC float easeOutElastic(float _t);

	/// In elastic.
	///
	///      ^
	///      |
	///      |      *****
	///      |      *   **
	///      |     **     **
	///      |    **       **         **********         **********         **
	///      |    *         ***   *****        ***********        ***********
	///      |   **           *****
	///      |   *
	///      |   *
	///      |  **
	///      |  *
	///      | **
	///      | *
	///      |**
	///      +*--------------------------------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeInElastic(float _t);

	/// In out elastic.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |                                   ***
	///      |                                  **  **    *****    ******    *
	///      |                                  *    ******   ******    ******
	///      |                                 *
	///      |                                 *
	///      |                                **
	///      |                                *
	///      |                               **
	///      |                               *
	///      |                              *
	///      |                              *
	///      +******----******----*****----**--------------------------------->
	///      |*    ******    ******   ***  *
	///      |                          ***
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeInOutElastic(float _t);

	/// Out in elastic.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                                               *
	///      |                                                               *
	///      |                                                              *
	///      |   ***                                                        *
	///      |  **  **    *****    ******    *******    ******    *****    **
	///      |  *    ******   ******    *******    ******    ******   ***  *
	///      | *                                                        ***
	///      | *
	///      |**
	///      +*--------------------------------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeOutInElastic(float _t);

	/// In back.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                                               *
	///      |                                                             **
	///      |                                                            **
	///      |                                                           **
	///      |                                                          **
	///      |                                                         **
	///      |                                                       **
	///      |                                                      **
	///      |                                                    **
	///      +*-------------------------------------------------***----------->
	///      |*************                                   ***
	///      |            *******                          ****
	///      |                  *******                *****
	///      |                        ******************
	///      |
	///
	BX_CONST_FUNC float easeInBack(float _t);

	/// Out back.
	///
	///      ^
	///      |
	///      |                      ******************
	///      |                  *****                *******
	///      |               ****                          *******
	///      |             ***                                   *************
	///      |           ***
	///      |          **
	///      |        ***
	///      |       **
	///      |     ***
	///      |    **
	///      |   **
	///      |  **
	///      | **
	///      +**-------------------------------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeOutBack(float _t);

	/// In out back.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |                                         **************
	///      |                                      ****            **********
	///      |                                     **
	///      |                                   ***
	///      |                                  **
	///      |                                 **
	///      |                                **
	///      |                               **
	///      |                             **
	///      |                            **
	///      |                           **
	///      +*------------------------**------------------------------------->
	///      |**********            ****
	///      |         **************
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeInOutBack(float _t);

	/// Out in back.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                                               *
	///      |                                                             **
	///      |                                                            **
	///      |         **************                                    **
	///      |      ****            ***********                        **
	///      |     **                         **********            ****
	///      |   ***                                   **************
	///      |  **
	///      | **
	///      +**-------------------------------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeOutInBack(float _t);

	/// Out bounce.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                                        ********
	///      |                                                     ****
	///      |                                                   ***
	///      |                                                 ***
	///      |                                               ***
	///      |                                              **
	///      |                                            **
	///      |                      **************       **
	///      |                   ****            ****   **
	///      +********************------------------****---------------------->
	///      |*     *
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeOutBounce(float _t);

	/// In bounce.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |                                                         *
	///      |                      ****                  ********************
	///      |                    ***  ****            ****
	///      |                   **       **************
	///      |                  **
	///      |                ***
	///      |              ***
	///      |            ***
	///      |          ***
	///      |       ****
	///      +********-------------------------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeInBounce(float _t);

	/// In out bounce.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |                                                            *
	///      |                                          *****     ************
	///      |                                         **   *******
	///      |                                       ***
	///      |                                     ***
	///      |                                ******
	///      |                          *******
	///      |                        ***
	///      |                       **
	///      |           *******   **
	///      +************------****------------------------------------------>
	///      |*  *
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeInOutBounce(float _t);

	/// Out in bounce.
	///
	///      ^
	///      |
	///      |
	///      |
	///      |
	///      |
	///      |                                                          ******
	///      |                                                        ***
	///      |                                                       **
	///      |                                           *******   **
	///      |                            *   ************      ****
	///      |          *****     *************  *
	///      |         **   *******
	///      |       ***
	///      |     ***
	///      +******---------------------------------------------------------->
	///      |*
	///      |
	///      |
	///      |
	///      |
	///
	BX_CONST_FUNC float easeOutInBounce(float _t);

} // namespace bx

#include "inline/easing.inl"

#endif // BX_EASING_H_HEADER_GUARD
