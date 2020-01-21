package com.unity3d.tinyplayer;

import java.util.concurrent.Semaphore;
import java.util.concurrent.TimeUnit;
import android.app.Activity;
import android.content.Context;
import android.content.res.AssetManager;
import android.util.Log;
import android.view.View;
import android.view.SurfaceView;
import android.view.SurfaceHolder;
import android.os.Handler;
import android.os.Message;
import android.os.Looper;

class UnityTinyView extends SurfaceView implements SurfaceHolder.Callback
{
    enum RunStateEvent { PAUSE, RESUME, QUIT, SURFACE_LOST, SURFACE_ACQUIRED, SURFACE_CHANGED, NEXT_FRAME };
    private static final int RUN_STATE_CHANGED_MSG_CODE = 2269;
    private static final int ANR_TIMEOUT_SECONDS = 4;

    private class UnityTinyThread extends Thread
    {
        Handler m_Handler;
        SurfaceHolder m_Holder;
        boolean m_Running = false;
        boolean m_SurfaceAvailable = false;

        public UnityTinyThread(SurfaceHolder holder)
        {
            m_Holder = holder;
        }

        @Override
        public void run()
        {
            setName("DOTSMain");

            Looper.prepare();
            m_Handler = new Handler(new Handler.Callback()
            {
                public boolean handleMessage(Message msg)
                {
                    if (msg.what != RUN_STATE_CHANGED_MSG_CODE)
                        return false;

                    final RunStateEvent runState = (RunStateEvent)msg.obj;
                    if (runState == RunStateEvent.NEXT_FRAME)
                    {
                        if (!m_Running)
                            return true;

                        if (!m_SurfaceAvailable)
                            return true;

                        UnityTinyAndroidJNILib.step();
                    }
                    else if (runState == RunStateEvent.QUIT)
                    {
                        Log.d(TAG, "Thread QUIT");
                        UnityTinyAndroidJNILib.destroy();
                        Looper.myLooper().quit();
                    }
                    else if (runState == RunStateEvent.RESUME)
                    {
                        Log.d(TAG, "Thread RESUME");
                        UnityTinyAndroidJNILib.pause(0);
                        m_Running = true;
                    }
                    else if (runState == RunStateEvent.PAUSE)
                    {
                        Log.d(TAG, "Thread PAUSE");
                        UnityTinyAndroidJNILib.pause(1);
                        m_Running = false;
                    }
                    else if (runState == RunStateEvent.SURFACE_LOST)
                    {
                        Log.d(TAG, "Thread SURFACE_LOST");
                        m_SurfaceAvailable = false;
                    }
                    else if (runState == RunStateEvent.SURFACE_ACQUIRED)
                    {
                        Log.d(TAG, "Thread SURFACE_ACQUIRED");
                    }
                    else if (runState == RunStateEvent.SURFACE_CHANGED)
                    {
                        Log.d(TAG, "Thread SURFACE_CHANGED");
                        UnityTinyAndroidJNILib.init(m_Holder.getSurface(), msg.arg1, msg.arg2);
                        m_SurfaceAvailable = true;
                    }

                    // trigger next frame
                    if (m_Running)
                        Message.obtain(m_Handler, RUN_STATE_CHANGED_MSG_CODE, RunStateEvent.NEXT_FRAME).sendToTarget();

                    return true;
                }
            });

            Log.d(TAG, "Thread JNILib.start call");
            UnityTinyAndroidJNILib.start();
            Log.d(TAG, "Thread JNILib.start after");

            Looper.loop();
        }

        public void quit()
        {
            dispatchRunStateEvent(RunStateEvent.QUIT);
        }

        public void resumeExecution()
        {
            dispatchRunStateEvent(RunStateEvent.RESUME);
        }

        public void pauseExecution(Runnable runnable)
        {
            if (m_Handler == null)
                return;
            dispatchRunStateEvent(RunStateEvent.PAUSE);
            Message.obtain(m_Handler, runnable).sendToTarget();
        }

        public void surfaceLost()
        {
            dispatchRunStateEvent(RunStateEvent.SURFACE_LOST);
        }

        public void surfaceAcquired()
        {
            dispatchRunStateEvent(RunStateEvent.SURFACE_ACQUIRED);
        }

        public void surfaceChanged(int width, int height)
        {
            if (m_Handler != null)
                Message.obtain(m_Handler, RUN_STATE_CHANGED_MSG_CODE, width, height, RunStateEvent.SURFACE_CHANGED).sendToTarget();
        }

        private void dispatchRunStateEvent(RunStateEvent ev)
        {
            if (m_Handler != null)
                Message.obtain(m_Handler, RUN_STATE_CHANGED_MSG_CODE, ev).sendToTarget();
        }

    }

    private static String TAG = "UnityTinyView";
    private UnityTinyThread m_Thread;

    public UnityTinyView(AssetManager assetManager, String path, Context context)
    {
        super(context);

        getHolder().addCallback(this);
        UnityTinyAndroidJNILib.setAssetManager(assetManager);

        m_Thread = new UnityTinyThread(getHolder());
        m_Thread.start();
    }

    @Override
    public void surfaceChanged(SurfaceHolder holder, int format, int width, int height)
    {
        Log.d(TAG, "surfaceChanged " + width + " x " + height);
        m_Thread.surfaceChanged(width, height);
    }

    @Override
    public void surfaceCreated(SurfaceHolder holder)
    {
        Log.d(TAG, "surfaceCreated");
        m_Thread.surfaceAcquired();
    }

    @Override
    public void surfaceDestroyed(SurfaceHolder holder)
    {
        Log.d(TAG, "surfaceDestroyed");
        m_Thread.surfaceLost();
    }

    public void onPause()
    {
        Log.d(TAG, "Pause");

        final Semaphore synchronize = new Semaphore(0);

        Runnable runnable = new Runnable() { public void run(){
                synchronize.release();
            }};

        m_Thread.pauseExecution(runnable);

        try
        {
            if (!synchronize.tryAcquire(ANR_TIMEOUT_SECONDS, TimeUnit.SECONDS))
            {
                Log.w(TAG, "Timeout while trying to pause the Unity Engine.");
            }
        }
        catch (InterruptedException e)
        {
            Log.w(TAG, "UI thread got interrupted while trying to pause the Unity Engine.");
        }
    }

    public void onResume()
    {
        Log.d(TAG, "Resume");
        m_Thread.resumeExecution();
    }

    public void onDestroy()
    {
        Log.d(TAG, "Destroy");
        m_Thread.quit();
    }
}
