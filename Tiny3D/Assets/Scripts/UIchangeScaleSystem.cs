
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
//using UnityEngine;
using Unity.Jobs;
#if UNITY_DOTSPLAYER
using Unity.Tiny.Rendering;
using Unity.Tiny.Input;
#endif
namespace Tiny3D
{
	public class UIchangeScaleSystem : JobComponentSystem
	{
		
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			
			return Entities.ForEach((ref UIPose uipose, in UIInputs ipt) =>
			{
				uipose.scale.x += ipt.HorizontalAxis;
				uipose.scale.y += ipt.VertAxis;

			}).Schedule(inputDeps);



		}
	}
}