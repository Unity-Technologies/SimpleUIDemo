namespace Unity.Tiny
{
    public enum LoopMode
    {
        /// <summary>
        ///  The value is looped. It goes from the min to the max value. When the value reaches the max value, it starts
        ///  from the beginning. It works in both directions.
        /// </summary> 
        Loop,

        /// <summary>
        ///  The value is clamped between min and max value. If the value is equal or larger than max value, the caller
        ///  should be notified about the end of the animation/sequence.
        /// </summary>
        Once,

        /// <summary>
        ///  The value goes between min and max back and forth.
        /// </summary>
        PingPong,

        /// <summary>
        ///  Same as PingPong, but performs only one cycle. If the value is equal or larger than max value, the caller
        ///  should be notified about the end of the animation/sequence.
        /// </summary>
        PingPongOnce,

        /// <summary>
        ///  The value is clamped between min and max value.
        /// </summary>
        ClampForever
    }
}
