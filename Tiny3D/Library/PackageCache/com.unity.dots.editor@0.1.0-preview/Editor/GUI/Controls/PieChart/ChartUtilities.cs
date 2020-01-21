using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Editor
{
    internal static class ChartUtilities
    {
        public static void Circle(Vector2 pos, float radius, Color color, MeshGenerationContext context)
        {
            Circle(pos, radius, 0, Mathf.PI * 2, color, context);
        }
        
        public static void Circle(Vector2 pos, float radius, float startAngle, float endAngle, Color color, MeshGenerationContext context)
        {
            var segments = 50;
            var mesh = context.Allocate(segments + 1, segments * 3);
            mesh.SetNextVertex(new Vertex()
            {
                position = new Vector3(pos.x, pos.y, -1),
                tint = color
            });
            var angle = startAngle;
            var range = endAngle - startAngle;
            var step = range / (segments-1);
           
            // store off the first position
            Vector3 offset = new Vector3(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle));
            mesh.SetNextVertex(new Vertex()
            {
                position = new Vector3(pos.x, pos.y, -1) + offset,
                tint = color
            });
           
            // calculate the rest of the arc/circle
            for (var i = 0; i < segments-1; i++)
            {
                angle += step;
                offset = new Vector3(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle));
                mesh.SetNextVertex(new Vertex()
                {
                    position = new Vector3(pos.x, pos.y, -1) + offset,
                    tint = color
                });
            }
           
            for (ushort i = 1; i < segments; i++)
            {
                mesh.SetNextIndex(0);
                mesh.SetNextIndex((ushort)(i+1));
                mesh.SetNextIndex(i);
            }
        }
    }
}