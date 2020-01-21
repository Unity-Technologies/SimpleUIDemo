using NUnit.Framework;
using Unity.Platforms.Windows;

class BasicTests
{
	[Test]
	public void VerifyCanReferenceWindowsBuildTarget()
	{
		Assert.IsNotNull(typeof(WindowsBuildTarget));
	}
}
