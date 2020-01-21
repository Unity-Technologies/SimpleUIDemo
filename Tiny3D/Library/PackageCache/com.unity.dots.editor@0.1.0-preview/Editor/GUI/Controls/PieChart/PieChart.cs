using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.Editor
{
    internal class PieChart : VisualElement
    {
        public enum LabelDataType
        {
            Title,
            Value,
            Percentage
        }

        public new class UxmlFactory : UxmlFactory<PieChart, UxmlTraits>
        {
            
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private UxmlStringAttributeDescription m_Title;
            private UxmlBoolAttributeDescription m_EnableLegend;
            private UxmlBoolAttributeDescription m_EnableLabels;
            private UxmlBoolAttributeDescription m_AllowLabelsOverflow;

            public UxmlTraits()
            {
                m_Title = new UxmlStringAttributeDescription {name = "title"};
                m_EnableLegend = new UxmlBoolAttributeDescription {name = "enableLegend"};
                m_EnableLabels = new UxmlBoolAttributeDescription {name = "enableLabels"};
                m_AllowLabelsOverflow = new UxmlBoolAttributeDescription {name = "allowLabelsOverflow"};
            }
            
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var chart = (PieChart) ve;
                chart.EnableLabels = m_EnableLabels.GetValueFromBag(bag, cc);
                chart.EnableLegend = m_EnableLegend.GetValueFromBag(bag, cc);
                chart.AllowLabelsOverflow = m_AllowLabelsOverflow.GetValueFromBag(bag, cc);
                chart.Title = m_Title.GetValueFromBag(bag, cc);
            }
        }

        private const int k_LabelsPadding = 15;

        private readonly Label m_Title;
        private readonly VisualElement m_Chart;
        private readonly VisualElement m_ChartLegend;
        private readonly DelayedTask m_RepaintTask;

        private bool m_EnableLabels = true;
        private bool m_AllowLabelsOverflow;
        private LabelDataType m_LabelsType = LabelDataType.Percentage;
        private LabelDataType m_LegendLabelsType = LabelDataType.Title;

        public List<PieChartSegment> Segments { get; } = new List<PieChartSegment>();
        
        public PieChart()
        {
            var template = new StyleSheets.UxmlTemplate("PieChart");
            
            styleSheets.Add(template.StyleSheet);
            AddToClassList("pie-chart");
            
            m_Title = new Label();
            m_Title.AddToClassList("pie-chart__title");

            var chartContainer = new VisualElement();
            chartContainer.AddToClassList("pie-chart__chart-container");
            Add(chartContainer);
            
            m_Chart = new VisualElement
            {
                name = "Chart"
            };
            
            m_Chart.AddToClassList("pie-chart__chart-view");
            chartContainer.Add(m_Chart);

            m_ChartLegend = new Box
            {
                name = "Legend"
            };
            
            m_ChartLegend.AddToClassList("pie-chart__legend");
            m_ChartLegend.style.marginTop = k_LabelsPadding;
            chartContainer.Add(m_ChartLegend);
            
            m_RepaintTask = new DelayedTask(this, () =>
            {
                UpdateChart();
                UpdateGeometry();
                foreach (var segment in Segments)
                {
                    segment.MarkDirtyRepaint();
                }
            });
            
            m_Chart.RegisterCallback<GeometryChangedEvent>(e => { UpdateGeometry(); });
        }

        public PieChart(IEnumerable<IChartSegmentData> segments):this()
        {
            foreach (var data in segments)
            {
               CreateSegment(data);
            }

            UpdateChart();
        }

        public string Title
        {
            get { return m_Title.text; }
            set
            {
                m_Title.text = value;
                if (string.IsNullOrEmpty(m_Title.text) && m_Title.parent != null)
                {
                    m_Title.RemoveFromHierarchy();
                }
                
                if (!string.IsNullOrEmpty(m_Title.text) && m_Title.parent == null)
                {
                    Insert(0, m_Title);
                }
            }
        }
        
        public bool EnableLegend
        {
            get => m_ChartLegend.parent != null;
            set
            {
                if (value == EnableLegend) return;
                
                if (value)
                {
                    m_Chart.parent.Add(m_ChartLegend);
                }
                else
                {
                    m_ChartLegend.RemoveFromHierarchy();
                }
            }
        }
        
        public bool EnableLabels
        {
            get => m_EnableLabels;
            set
            {
                if(m_EnableLabels == value) return;
                m_EnableLabels = value;
                Repaint();
            }
        }
        
        public bool AllowLabelsOverflow
        {
            get => m_AllowLabelsOverflow;
            set
            {
                if(m_AllowLabelsOverflow == value) return;
                m_AllowLabelsOverflow = value;
                Repaint();
            }
        }
        
        public LabelDataType LabelsType
        {
            get => m_LabelsType;
            set
            {
                if(m_LabelsType == value) return;
                m_LabelsType = value;
                Repaint();
            }
        }
        
        public LabelDataType LegendLabelsType
        {
            get => m_LegendLabelsType;
            set
            {
                if(m_LegendLabelsType == value) return;
                m_LegendLabelsType = value;
                Repaint();
            }
        }
        
        public void Repaint()
        {
           m_RepaintTask.Execute();
        }
        public void AddSegment(IChartSegmentData data)
        {
            CreateSegment(data);
            Repaint();
        }

        public void RemoveSegment(PieChartSegment segment)
        {
            Assert.IsTrue(Segments.Contains(segment));
            var index = Segments.IndexOf(segment);
            m_ChartLegend.Children().ToArray()[index].RemoveFromHierarchy();
            segment.RemoveFromHierarchy();
            Segments.Remove(segment);
            Repaint();
        }

        private void UpdateGeometry()
        {
            var chartPadding = 0f;
            Segments.ForEach(segment =>
            {
                segment.style.width = m_Chart.localBound.width;
                segment.style.height = m_Chart.localBound.height;

                if (m_EnableLabels && !m_AllowLabelsOverflow)
                {
                    var labelPosition = segment.GetLabelPosition(GetChartRadius(m_Chart.localBound), GetChartCenter(m_Chart.localBound));
                    var labelBounds = new Rect(labelPosition, segment.Label.localBound.size);
                    var labelRequiredPadding = GetLabelRequiredPadding(m_Chart.localBound, labelBounds);
                    chartPadding = Mathf.Max(chartPadding, labelRequiredPadding);
                }
            });
            
            Segments.ForEach(segment =>
            {
                segment.Label.visible = m_EnableLabels;
                var radius = GetChartRadius(m_Chart.localBound) - chartPadding;
                if (m_EnableLabels)
                {
                    var labelPosition = segment.GetLabelPosition(radius, GetChartCenter(m_Chart.localBound));
                    segment.Label.style.left = labelPosition.x;
                    segment.Label.style.top = labelPosition.y;
                    radius -= k_LabelsPadding;
                }
               
                segment.SetChartRadius(radius);
            });
        }

        internal void OnSegmentAdded(PieChartSegment segment)
        {
            AttachSegment(segment);
            Repaint();
        }

        private void CreateSegment(IChartSegmentData data)
        {
            var segment = new PieChartSegment(data); 
            AttachSegment(segment);
        }

        private void AttachSegment(PieChartSegment segment)
        {
            m_Chart.Add(segment);
            segment.AddToClassList("pie-chart__chart-segment");

            //The chart will be repainted every time a label is updated.
            //Ideally is to avoid duplicated updates.
            segment.Label.RegisterCallback<GeometryChangedEvent>(e => { Repaint(); });
            Segments.Add(segment);

            var labelRow = new VisualElement();
            labelRow.AddToClassList("legend__labels-raw");
            m_ChartLegend.Add(labelRow);
            
            var colorBox = new Box();
            colorBox.AddToClassList("labels-raw__color-box");
            colorBox.style.backgroundColor = segment.Data.Color;
            labelRow.Add(colorBox);
            
            var segmentLabel = new Label();
            labelRow.AddToClassList("labels-raw__label");
            labelRow.Add(segmentLabel);
        }

        private void UpdateChart()
        {
            double sum = 0;
            Segments.ForEach(segment => { sum += segment.Data.DoubleValue / Segments.Count; });
            
            float startAngle = 0;
            var legendLabels = m_ChartLegend.Children().ToList();
            for (var i = 0; i < Segments.Count; i++)
            {
                var segment = Segments[i];
                var segmentDegree = (float) (segment.Data.DoubleValue / Segments.Count / sum * 2 * Mathf.PI);
                var endAngle = segmentDegree + startAngle;
                
                segment.SetAngles(startAngle, endAngle);
                startAngle = endAngle;

                segment.Label.text = segment.GetValue(m_LabelsType);
                legendLabels[i].Q<Label>().text = segment.GetValue(m_LegendLabelsType);
                legendLabels[i].Q<Box>().style.backgroundColor = segment.Data.Color;
            }
        }
        
        internal static Vector2 GetChartCenter(Rect bounds)
        {
            return new Vector2(bounds.width / 2f,  GetChartRadius(bounds));
        }
        
        private static float GetLabelRequiredPadding(Rect viewBounds, Rect labelBounds)
        {
            var paddingX = 0f;
            var paddingY = 0f;
            
            //width overflow
            if (labelBounds.xMin < 0)
            {
                paddingX = Mathf.Max(paddingX, Mathf.Abs(labelBounds.xMin));
            } else if(labelBounds.xMax > viewBounds.width)
            {
                paddingX = Mathf.Max(paddingX, labelBounds.xMax - viewBounds.width);
            }

            //height overflow
            if (labelBounds.yMin < 0)
            {
                paddingY = Mathf.Max(paddingY, Mathf.Abs(labelBounds.yMin));
            } else if(labelBounds.yMax > viewBounds.height)
            {
                paddingY = Mathf.Max(paddingY, labelBounds.yMax - viewBounds.height);
            }

            return Mathf.Max(paddingX, paddingY);
        }
        
        private static float GetChartRadius(Rect bounds)
        {
            return Mathf.Min(bounds.width, bounds.height) / 2f;
        }
    }
}
