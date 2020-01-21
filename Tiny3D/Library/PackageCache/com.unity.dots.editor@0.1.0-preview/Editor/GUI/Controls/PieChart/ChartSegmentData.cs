using System;
using UnityEngine;

namespace Unity.Editor
{
    internal class ChartSegmentData<TValue> : IChartSegmentData
    {
        public TValue Value { get; set; }
        public string Title { get; set; }
        public Color Color { get; set; }

        public double DoubleValue
        {
            get { return Convert.ToDouble(Value); }
        }
    }
}