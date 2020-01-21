#include <Unity/Runtime.h>

#include <GLFW/glfw3.h>
#if defined(WIN32)
#define GLFW_EXPOSE_NATIVE_WIN32
#elif defined(__APPLE__)
#define GLFW_EXPOSE_NATIVE_COCOA
#elif defined(__EMSCRIPTEN__)
#error Should not be here for Emscripten
#else
#define GLFW_EXPOSE_NATIVE_X11
#endif
#include <GLFW/glfw3native.h>
#include <stdio.h>
#include <math.h>
#include <vector>
#include <stdlib.h>

// TODO: could keep array of windows, window class etc.
// for now one static window is perfectly fine
static GLFWwindow* mainWindow = 0;
static bool shouldClose = false;
static bool initialized = false;

// input
static std::vector<int> mouse_pos_stream;
static std::vector<int> mouse_button_stream;
static std::vector<int> key_stream;

static int windowW = 0;
static int windowH = 0;

// callbacks
static void
window_size_callback(GLFWwindow* window, int width, int height)
{
    //printf ( "GLFW resize %i, %i\n", width, height);
    windowW = width;
    windowH = height;
}

static void
window_close_callback(GLFWwindow* window)
{
    shouldClose = true;
}

static void
cursor_position_callback(GLFWwindow* window, double xpos, double ypos)
{
    //printf ( "GLFW C mouse pos %f, %f\n", (float)xpos, (float)ypos);
    mouse_pos_stream.push_back((int)xpos);
    mouse_pos_stream.push_back(windowH - 1 - (int)ypos);
}

static void
mouse_button_callback(GLFWwindow* window, int button, int action, int mods)
{
    //printf ( "GLFW C mouse button %i, %i, %i\n", button, action, mods);
    mouse_button_stream.push_back(button);
    mouse_button_stream.push_back(action);
    mouse_button_stream.push_back(mods);
}

static void
key_callback(GLFWwindow* window, int key, int scancode, int action, int mods)
{
    //printf ( "GLFW C key %i, %i, %i, %i\n", key, scancode, action, mods);
    key_stream.push_back(key);
    key_stream.push_back(scancode);
    key_stream.push_back(action);
    key_stream.push_back(mods);
}

static void
error_callback(int error, const char* description)
{
    printf("GLFW error %d : %s\n", error, description);
}

// exports to c#
DOTS_EXPORT(bool)
init_glfw()
{
    if (initialized)
        return true;

    glfwSetErrorCallback(error_callback);

    if (!glfwInit()) {
        printf("GLFW init failed.\n");
        return false;
    }

    initialized = true;
    return true;
}

DOTS_EXPORT(bool)
create_window_glfw(int width, int height)
{
    if (!initialized)
    {
        printf("Not initialized!");
        return false;
    }

    if (!mainWindow)
    {
        glfwWindowHint (GLFW_CLIENT_API, GLFW_NO_API);
        mainWindow = glfwCreateWindow(width, height, "Unity - DOTS Project", NULL, NULL);
        if (!mainWindow) {
            printf("GLFW window creation failed.\n");
            return false;
        }
    } else {
        // this is fine, window already exists
        glfwSetWindowSize(mainWindow, width, height);
    }

    windowW = width;
    windowH = height;

    glfwSetWindowCloseCallback(mainWindow, window_close_callback);
    //glfwSetWindowUserPointer(mainWindow, this);
    glfwSetWindowSizeCallback(mainWindow, window_size_callback);

    return true;
}

DOTS_EXPORT(void)
show_window_glfw(int show)
{
    if (!mainWindow)
        return;

    if (show)
        glfwShowWindow(mainWindow);
    else
        glfwHideWindow(mainWindow);
}

DOTS_EXPORT(GLFWwindow *)
getwindow_glfw()
{
    return mainWindow;
}

DOTS_EXPORT(void)
getWindowSize_glfw(int *width, int *height)
{
    if (!mainWindow)
    {
        *width = 0;
        *height = 0;
        return;
    }
    glfwGetWindowSize(mainWindow, width, height);
    windowW = *width;
    windowH = *height;
}

DOTS_EXPORT(void)
getWindowFrameSize_glfw(int *left, int *top, int *right, int *bottom)
{
    if (!mainWindow)
    {
        *left = 0;
        *top = 0;
        *right = 0;
        *bottom = 0;
        return;
    }
    glfwGetWindowFrameSize(mainWindow, left, top, right, bottom);
}

DOTS_EXPORT(void)
getScreenSize_glfw(int *width, int *height)
{
    if (!mainWindow)
    {
        *width = 0;
        *height = 0;
        return;
    }
    GLFWmonitor* monitor = glfwGetWindowMonitor (mainWindow);
    if (!monitor)
        monitor = glfwGetPrimaryMonitor();
    const GLFWvidmode* mode = glfwGetVideoMode(monitor);
    *width = mode->width;
    *height = mode->height;
}

DOTS_EXPORT(void)
getFramebufferSize_glfw(int *width, int *height)
{
    if (!mainWindow) 
    {
        *width = 0;
        *height = 0;
        return;
    }
    glfwGetWindowSize(mainWindow, width, height);
}

DOTS_EXPORT(void)
destroy_window_glfw()
{
    if (mainWindow) {
        glfwDestroyWindow(mainWindow);
        mainWindow = 0;
    }
}

DOTS_EXPORT(void)
shutdown_glfw(int exitCode)
{
    initialized = false;
    glfwTerminate();
}

DOTS_EXPORT(void)
resize_glfw(int width, int height)
{
    glfwSetWindowSize(mainWindow, width, height);
    windowW = width;
    windowH = height;
}

DOTS_EXPORT(bool)
messagePump_glfw()
{
    if (!mainWindow || shouldClose)
        return false;
    glfwPollEvents();
    return !shouldClose;
}

DOTS_EXPORT(double)
time_glfw()
{
    return glfwGetTime();
}

// exports to c#
DOTS_EXPORT(bool)
init_glfw_input()
{
    if (!mainWindow) {
        printf("GLFW main window not created.\n");
        return false;
    }
    glfwSetKeyCallback(mainWindow, key_callback);
    glfwSetCursorPosCallback(mainWindow, cursor_position_callback);
    glfwSetMouseButtonCallback(mainWindow, mouse_button_callback);
    return true;
}

DOTS_EXPORT(void)
reset_glfw_input()
{
    mouse_pos_stream.clear();
    mouse_button_stream.clear();
    key_stream.clear();
}

DOTS_EXPORT(const int *)
get_mouse_pos_stream_glfw_input(int *len)
{
    *len = (int)mouse_pos_stream.size();
    return mouse_pos_stream.data();
}

DOTS_EXPORT(const int *)
get_mouse_button_stream_glfw_input(int *len)
{
    *len = (int)mouse_button_stream.size();
    return mouse_button_stream.data();
}

DOTS_EXPORT(const int *)
get_key_stream_glfw_input(int *len)
{
    *len = (int)key_stream.size();
    return key_stream.data();
}

DOTS_EXPORT(void *)
glfw_get_platform_window_handle()
{
#if defined(WIN32) 
    return glfwGetWin32Window(mainWindow);
#elif defined(__APPLE__)
    return glfwGetCocoaWindow(mainWindow);
#else
return glfwGetX11Window(mainWindow);
#endif
}
