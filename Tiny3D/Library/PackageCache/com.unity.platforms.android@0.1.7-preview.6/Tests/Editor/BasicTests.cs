using NUnit.Framework;
using Unity.Platforms.Android;

class BasicTests
{
	[Test]
	public void VerifyCanReferenceAndroidBuildTarget()
	{
		Assert.IsNotNull(typeof(AndroidBuildTarget));
	}
}
