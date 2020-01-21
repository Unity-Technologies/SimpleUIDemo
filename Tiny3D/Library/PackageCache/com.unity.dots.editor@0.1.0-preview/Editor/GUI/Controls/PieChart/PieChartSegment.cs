using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Editor
{
    internal class PieChartSegment : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<PieChartSegment, UxmlTraits>
        {
            
        }

        /// <summary>
        ///   <para>UxmlTraits for the Label.</para>
        /// </summary>
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            private UxmlStringAttributeDescription m_Title;
            private UxmlFloatAttributeDescription m_Value;

            public UxmlTraits()
            {
                m_Title = new UxmlStringAttributeDescription { name = "title" };
                m_Value = new UxmlFloatAttributeDescription { name = "value" };
            }
            
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                var segment = (PieChartSegment) ve;
                var data = new ChartSegmentData<float>
                {
                    Title = m_Title.GetValueFromBag(bag, cc),
                    Value = m_Value.GetValueFromBag(bag, cc)
                };

                segment.Data = data;
            }
        }

        private float m_StartAngle;
        private float m_EndAngle;
        private float m_ChartRadius;
        private static readonly CustomStyleProperty<Color> s_SegmentColorProperty = new CustomStyleProperty<Color>("--chart-segment-color");
        
        internal Label Label { get; }
        public IChartSegmentData Data { get; set; }

        public PieChartSegment()
        {
            Label = new Label();
            Label.AddToClassList("chart-segment__label");
            Add(Label);
            
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }
        
        public PieChartSegment(IChartSegmentData data): this()
        {
            Data = data;
        }
        
        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (parent is PieChart pieChart)
            {
                pieChart.OnSegmentAdded(this);
            }
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (Data == null) return;
            if (evt.customStyle.TryGetValue(s_SegmentColorProperty, out var color))
            {
                Data.Color = color;
                MarkDirtyRepaint();
            }
        }
        
        public string GetValue(PieChart.LabelDataType dataType)
        {
            switch (dataType)
            {
                case PieChart.LabelDataType.Title:
                    return Data.Title;
                case PieChart.LabelDataType.Value:
                    return Data.DoubleValue.ToString(CultureInfo.InvariantCulture);
                case PieChart.LabelDataType.Percentage:
                    return ((m_EndAngle- m_StartAngle) / (Mathf.PI * 2)).ToString("P");
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, $"Provided {nameof(PieChart.LabelDataType)} is not supported.");
            }
        }

        internal void SetAngles(float startAngle, float endAngle)
        {
            m_StartAngle = startAngle;
            m_EndAngle = endAngle;
        }

        internal void SetChartRadius(float radius)
        {
            m_ChartRadius = radius;
        }

        internal Vector2 GetLabelPosition(float radius, Vector2 center)
        {
            var labelAngle = m_StartAngle + (m_EndAngle - m_StartAngle) / 2f;
            var labelX = center.x + radius * Mathf.Cos(labelAngle);
            var labelY = center.y + radius * Mathf.Sin(labelAngle);
            
            if (labelAngle > Mathf.PI / 2f && labelAngle < Mathf.PI * 1.5f)
            {
                labelX -= Label.localBound.width;  
            }
            labelY -= Label.localBound.height / 2f;
            
            return new Vector2(labelX, labelY);
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var center = PieChart.GetChartCenter(localBound);

            if (m_ChartRadius < 0)
            {
                return;
            }
            ChartUtilities.Circle(center, m_ChartRadius, m_StartAngle, m_EndAngle, Data.Color, mgc);
        }
    }
}