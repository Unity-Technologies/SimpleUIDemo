using UnityEngine;

namespace Unity.Editor
{
    internal interface IChartSegmentData
    {
        string Title { get; }
        Color Color { get; set; }
        double DoubleValue { get; }
    }
}