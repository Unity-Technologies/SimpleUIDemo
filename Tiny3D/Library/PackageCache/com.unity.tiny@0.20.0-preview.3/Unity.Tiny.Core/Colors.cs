namespace Unity.Tiny
{
    public static class Colors
    {
        static public Color White { get; } = NewColor(1, 1, 1, 1);
        static public Color Red { get; } = NewColor(1, 0, 0, 1);
        static public Color Cyan { get; } = NewColor(0, 1, 1, 1);
        static public Color Yellow { get; } = NewColor(1, 1, 0, 1);
        static public Color Green { get; } = NewColor(0, 1, 0, 1);
        static public Color Blue { get; } = NewColor(0, 0, 1, 1);
        static public Color DarkBlue { get; } = NewColor(0, 0, 0.4f, 1.0f);
        static public Color Orange { get; } = NewColor(1, 0.5f, 0, 1);
        static public Color Black { get; } = NewColor(0, 0, 0, 1);
        static public Color Transparent { get; } = NewColor(0, 0, 0, 0);
        static public Color Alpha30 { get; } = NewColor(1, 1, 1, 0.3f);
        static public Color NewColor(float r, float g, float b, float a)
        {
            return new Color()
            {
                r = r,
                g = g,
                b = b,
                a = a
            };
        }
    }
}
