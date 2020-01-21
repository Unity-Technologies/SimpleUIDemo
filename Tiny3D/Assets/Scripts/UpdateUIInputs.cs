using Unity.Entities;
using Unity.Mathematics;
using Unity.Tiny;
#if UNITY_DOTSPLAYER
using System;
using Unity.Tiny.Input;

#else
using UnityEngine;
#endif

namespace Tiny3D
{

    [AlwaysUpdateSystem]
    public class UpdateUIInputs : ComponentSystem
    {
        protected override void OnUpdate()
        {
            var subh = false;
            var addh = false;
            var subvert = false;
            var addvert = false;
#if UNITY_DOTSPLAYER
            var Input = World.GetExistingSystem<InputSystem>();
            
#endif

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                subvert = true;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                addvert = true;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                subh = true;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                addh = true;

#if !UNITY_DOTSPLAYER
			//UnityEngine.Debug.Log("update");
			if (Input.GetMouseButton(0))
                PressAtPosition(new float2(Input.mousePosition.x, Input.mousePosition.y), ref subh, ref addh, ref subvert, ref addvert);

            for (int i = 0; i < Input.touchCount; i++)
            {
                var pos = Input.GetTouch(i).position;
                PressAtPosition(new float2(pos.x, pos.y), ref subh, ref addh, ref subvert, ref addvert);
            }
#else
            if (Input.IsTouchSupported() && Input.TouchCount() > 0)
            {
                for (var i = 0; i < Input.TouchCount(); i++)
                {
                    var itouch = Input.GetTouch(i);
                    var pos = new float2(itouch.x, itouch.y);
                    PressAtPosition(pos, ref subh, ref addh, ref subvert, ref addvert);
                }
            }
            else
            {
                if (Input.GetMouseButton(0))
                {
                    var xpos = (int) Input.GetInputPosition().x;
                    PressAtPosition(Input.GetInputPosition(), ref subh, ref addh, ref subvert, ref addvert);
                }
            }
#endif

            UIInputs inputs = default;
            if (subh)
                inputs.HorizontalAxis = -0.1f;
            else if (addh)
                inputs.HorizontalAxis = 0.1f;

            if (addvert)
                inputs.VertAxis = 0.1f;
            else if (subvert)
                inputs.VertAxis = -0.1f;

            Entities.ForEach((ref UIInputs iv) => { iv = inputs; });
        }

        private void PressAtPosition(float2 inputScreenPosition, ref bool issubhPressed, ref bool isaddhPressed,
            ref bool issubvertPressed, ref bool isaddvertPressed)
        {
            // Determine which button is pressed byt checking the x value of the screen position.
            // TODO: Replace this with a UI interaction system

#if !UNITY_DOTSPLAYER
            int width = Screen.width;
			int height = Screen.height;
#else
            var di = GetSingleton<DisplayInfo>();

            // TODO currently rendering is done with 1080p, with aspect kept.
            // We might not be using the actual width.  DisplayInfo needs to get reworked.
            int height = di.height;
            int width = di.width;
            float targetRatio = 1920.0f / 1080.0f;
            float actualRatio = (float) width / (float) height;
            if (actualRatio > targetRatio)
            {
                width = (int) (di.height * targetRatio);
                inputScreenPosition.x -= (di.width - width) / 2.0f;
            }
            // if height > width, then the full width will get used for display
#endif

            var screenRatiox = inputScreenPosition.x / width;
			var screenRatioy = inputScreenPosition.y / height;
			if (screenRatioy > 0.7f)
                isaddvertPressed = true;
            else if(screenRatioy<0.3f)
                issubvertPressed = true;
            if (screenRatiox <0.3f)
                issubhPressed = true;
            else if(screenRatiox>0.7f)
                isaddhPressed = true;
        }
    }
}