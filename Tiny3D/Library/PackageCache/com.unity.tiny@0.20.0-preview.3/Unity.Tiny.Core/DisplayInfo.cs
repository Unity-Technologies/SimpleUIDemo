using Unity.Entities;

namespace Unity.Tiny
{
    public enum ScreenOrientation
    {
        Portrait = 1,
        PortraitUpsideDown = 2,
        Landscape = 3,
        LandscapeLeft = 3,
        LandscapeRight = 4,
        AutoRotation = 5
    }

    /// <summary>
    ///  Configures display-related parameters. You can access this component via
    ///  TinyEnvironment.Get/SetConfigData&lt;DisplayInfo&gt;()
    /// </summary>
    //[HideInInspector]
    public struct DisplayInfo : IComponentData
    {
        public static DisplayInfo Default { get; } = new DisplayInfo
        {
            width = 1280,
            height = 720,
            autoSizeToFrame = true
        };

        /// <summary>
        /// Specifies the output width, in logical pixels. Writing will resize the window, where supported.
        /// </summary>
        public int width;

        /// <summary>
        /// Specifies the output height, in logical pixels.
        /// Writing will resize the window, where supported.
        /// </summary>
        public int height;

        /// <summary>
        /// Specifies the output height, in physical pixels.
        /// Read-only, but it can be useful for shaders or texture allocations.
        /// </summary>
        public int framebufferHeight;

        /// <summary>
        /// Specifies the output width, in physical pixels.
        /// Read-only, but it can be useful for shaders or texture allocations.
        /// </summary>
        public int framebufferWidth;

        /// <summary>
        ///  If set to true, the output automatically resizes to fill the frame
        ///  (the browser or application window), and match the orientation.
        ///  Changing output width or height manually has no effect.
        /// </summary>
        public bool autoSizeToFrame;

        /// <summary>
        ///  Specifies the frame width, in pixels. This is the width of the browser
        ///  or application window.
        /// </summary>
        public int frameWidth;

        /// <summary>
        ///  Specifies the frame height, in pixels. This is the height of the browser
        ///  or application window.
        /// </summary>
        public int frameHeight;

        /// <summary>
        ///  Specifies the device display (screen) width, in pixels.
        /// </summary>
        public int screenWidth;

        /// <summary>
        ///  Specifies the device display (screen) height, in pixels.
        /// </summary>
        public int screenHeight;

        /// <summary>
        ///  Specifies the scale of the device display (screen) DPI relative to.
        ///  96 DPI. For example, a value of 2.0 yields 192 DPI (200% of 96).
        /// </summary>
        public float screenDpiScale;

        /// <summary>
        ///  Specifies the device display (screen) orientation.
        /// </summary>
        public ScreenOrientation orientation;

        /// <summary>
        ///  Specifies whether the browser or application window has focus.
        ///  Read only; setting this value has no effect.
        /// </summary>
        public bool focused;

        /// <summary>
        ///  Specifies whether the browser or application window is currently visible
        ///  on the screen/device display.
        ///  Read only; setting this value has no effect.
        /// </summary>
        public bool visible;

        /// <summary>
        ///  Specifies whether swapping should not wait for vertical sync.
        ///  By default rendering will wait for sync is.
        ///  Disabling the wait for sync might not be possible on some platforms, like inside a web browser.
        /// </summary>
        public bool disableVSync;

        /// <summary>
        ///  Disable SRGB encodings. 
        ///  Filtering and blending will happen in non-linear (gamma) space, which is wrong but more performant on 
        ///  older devices. Also the srgb texture flag is ignored for texture reads. 
        ///  Enabling this flag is the equivalent of selecting Gamma workflow. 
        /// </summary>
        public bool disableSRGB;
    }
}
