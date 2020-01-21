package com.unity3d.tinyplayer;

import android.app.Activity;
import android.os.Bundle;
import android.os.Process;
import android.util.Log;
import android.view.MotionEvent;
import android.view.View;
import android.view.Window;
import android.view.WindowManager;
import android.view.KeyEvent;
import android.content.res.AssetManager;

import java.io.File;

public class UnityTinyActivity extends Activity {

    UnityTinyView mView;
    AssetManager mAssetManager;

    @Override protected void onCreate(Bundle bundle) {
        super.onCreate(bundle);

        requestWindowFeature(Window.FEATURE_NO_TITLE);
        mAssetManager = getAssets();
        mView = new UnityTinyView(mAssetManager, getCacheDir().getAbsolutePath(), this);
        mView.setOnTouchListener(new View.OnTouchListener() {

            public boolean onTouch(View v, MotionEvent event) {
                int action = event.getActionMasked();
                switch (action) {
                    case MotionEvent.ACTION_DOWN:
                    case MotionEvent.ACTION_POINTER_DOWN:
                    case MotionEvent.ACTION_UP:
                    case MotionEvent.ACTION_POINTER_UP:
                    case MotionEvent.ACTION_CANCEL: {
                        int index = event.getActionIndex();
                        if (action == MotionEvent.ACTION_POINTER_DOWN) action = MotionEvent.ACTION_DOWN;
                        else if (action == MotionEvent.ACTION_POINTER_UP) action = MotionEvent.ACTION_UP;
                        UnityTinyAndroidJNILib.touchevent(event.getPointerId(index), action, (int)event.getX(index), (int)event.getY(index));
                    }
                        break;
                    case MotionEvent.ACTION_MOVE: {
                        for (int i = 0; i < event.getPointerCount(); ++i) {
                            int pointerId = event.getPointerId(i);
                            UnityTinyAndroidJNILib.touchevent(pointerId, action, (int)event.getX(i), (int)event.getY(i));
                        }
                    }
                        break;
                }
                return true;
            }
        });
        setContentView(mView);
        mView.requestFocus();
    }

    @Override protected void onPause() {
        mView.onPause();
        super.onPause();
    }

    @Override protected void onResume() {
        super.onResume();
        mView.onResume();
    }

    @Override protected void onDestroy() {
        mView.onDestroy();
        super.onDestroy();
        Process.killProcess(Process.myPid());
    }

    @Override public boolean onKeyUp(int keyCode, KeyEvent event)
    {
        UnityTinyAndroidJNILib.keyevent(event.getKeyCode(), event.getScanCode(), event.getAction(), event.getMetaState());
        // volume up/down keys need to be processed by system
        return event.getKeyCode() != KeyEvent.KEYCODE_VOLUME_DOWN &&
               event.getKeyCode() != KeyEvent.KEYCODE_VOLUME_UP;
    }

    @Override public boolean onKeyDown(int keyCode, KeyEvent event)
    {
        UnityTinyAndroidJNILib.keyevent(event.getKeyCode(), event.getScanCode(), event.getAction(), event.getMetaState());
        // volume up/down keys need to be processed by system
        return event.getKeyCode() != KeyEvent.KEYCODE_VOLUME_DOWN &&
               event.getKeyCode() != KeyEvent.KEYCODE_VOLUME_UP;
    }
}
