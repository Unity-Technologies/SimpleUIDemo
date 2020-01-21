using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.Editor.Tests.UITests
{
    public class PieChartTests
    {
        private class TestWindow : EditorWindow
        {
            public void OnEnable()
            {
                var chart = MakeChart();
                chart.style.flexGrow = 1;
                rootVisualElement.Add(chart);
            }
            
            private static PieChart MakeChart()
            {
                var chart = new PieChart(new []
                {
                    new ChartSegmentData<double>
                    {
                        Title = "Segment 1",
                        Value = 250.5,
                        Color = Color.blue
                    },
            
                    new ChartSegmentData<double>
                    {
                        Title = "Segment 2",
                        Value = 250.5, 
                        Color = Color.red
                    },
                    
                    new ChartSegmentData<double>
                    {
                        Title = "Segment 3",
                        Value = 250.5, 
                        Color = Color.red
                    },
            
                    new ChartSegmentData<double>
                    {
                        Title = "Segment 4",
                        Value  = 500.5,
                        Color = Color.green
                    },
                    
                    new ChartSegmentData<double>
                    {
                        Title = "Segment 5",
                        Value  = 500.5,
                        Color = Color.green
                    },
                });
        
                return chart;
            }
        }

        private TestWindow m_Window;
        private PieChart m_Chart;
        private VisualElement m_ChartView;
        private Box m_ChartLegend;
        
        private readonly List<string> m_Names = new List<string> { "Segment 1", "Segment 2", "Segment 3", "Segment 4", "Segment 5" };
        private readonly List<string> m_Values = new List<string> { "250.5", "250.5", "250.5", "500.5", "500.5" };
        private readonly List<string> m_Presents = new List<string> { "14.29 %", "14.29 %", "14.29 %", "28.56 %", "28.56 %" };

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            m_Window = EditorWindow.GetWindow<TestWindow>();
            m_Window.position = new Rect(0, 0, 300, 300);
            m_Window.Show();
            
            m_Chart = m_Window.rootVisualElement.Q<PieChart>();
            m_ChartView = m_Chart.Q<VisualElement>("Chart");
            m_ChartLegend = m_Chart.Q<Box>("Legend");

            yield return null;
        }
        
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            m_Window.Close();
            yield return null;
        }
        
        /// <summary>
        /// TODO Revisit once we have a way to run a task on a next frame.
        /// Currently, <see cref="IVisualElementScheduler"/> only guarantees that the task will be executed... shortly.  
        /// </summary>
        private IEnumerator WaitForChartUpdate()
        {
            const int framesDelay = 5;
            for (var i = 0; i < framesDelay - 1; i++)
            {
                yield return null;
            }
            
            yield return null;
        }
        
        [UnityTest]
        [Ignore("Temporary ignored due to UIE renderer issue.")]
        public IEnumerator TestSegments()
        {
            var index = 0;
            m_Chart.LabelsType = PieChart.LabelDataType.Percentage;
            m_ChartView.Query<Label>().ForEach(label =>
            {
                Assert.IsTrue(label.text.Equals(m_Presents[index]));
                index++;
            });
            
            index = 0;
            m_Chart.LabelsType = PieChart.LabelDataType.Value;
            
            yield return WaitForChartUpdate();
            m_ChartView.Query<Label>().ForEach(label =>
            {
                Assert.IsTrue(label.text.Equals(m_Values[index]));
                index++;
            });
            
            index = 0;
            m_Chart.LabelsType = PieChart.LabelDataType.Title;
            
            yield return WaitForChartUpdate();
            m_ChartView.Query<Label>().ForEach(label =>
            {
                Assert.IsTrue(label.text.Equals(m_Names[index]));
                index++;
            });

            m_Chart.EnableLegend = true;
            var expectedSegmentsCount = m_Values.Count;
            Assert.IsTrue(expectedSegmentsCount == m_ChartView.Query<PieChartSegment>().ToList().Count);
            Assert.IsTrue(expectedSegmentsCount == m_ChartLegend.childCount);
            
            var segment = m_Chart.Segments[0];
            m_Chart.RemoveSegment(segment);
            
            expectedSegmentsCount--;
            Assert.IsTrue(expectedSegmentsCount == m_ChartView.Query<PieChartSegment>().ToList().Count);
            Assert.IsTrue(expectedSegmentsCount == m_ChartLegend.childCount);
            
            m_Chart.AddSegment(new ChartSegmentData<float>
            {
                Title = "New Segment",
                Value = 500f,
                Color = Color.red
            });
            
            expectedSegmentsCount++;
            Assert.IsTrue(expectedSegmentsCount == m_ChartView.Query<PieChartSegment>().ToList().Count);
            Assert.IsTrue(expectedSegmentsCount == m_ChartLegend.childCount);

            segment = new PieChartSegment
            {
                Data = new ChartSegmentData<float> {Title = "New Segment", Value = 500f, Color = Color.red}
            };
            m_Chart.Add(segment);
            
            expectedSegmentsCount++;
            Assert.IsTrue(expectedSegmentsCount == m_ChartView.Query<PieChartSegment>().ToList().Count);
            Assert.IsTrue(expectedSegmentsCount == m_ChartLegend.childCount);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestProperties()
        {
            m_Chart.EnableLabels = true;
            var chartLabels = m_ChartView.Query<Label>().ToList();
            Assert.IsNotEmpty(chartLabels);
            foreach (var label in chartLabels)
            {
                Assert.IsTrue(label.visible);
            }
            
            m_Chart.EnableLabels = false;
            
            yield return WaitForChartUpdate();
            foreach (var label in chartLabels)
            {
                Assert.IsFalse(label.visible);
            }

            m_Chart.EnableLegend = true;
            Assert.IsNotNull(m_Chart.Q<Box>("Legend").parent);
            
            m_Chart.EnableLegend = false;
            Assert.IsNull(m_Chart.Q<Box>("Legend"));
            
            yield return null;
        }
    }
}