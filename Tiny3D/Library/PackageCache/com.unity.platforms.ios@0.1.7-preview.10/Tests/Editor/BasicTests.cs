using NUnit.Framework;
using Unity.Platforms.iOS;

class BasicTests
{
	[Test]
	public void VerifyCanReferenceiOSBuildTarget()
	{
		Assert.IsNotNull(typeof(iOSBuildTarget));
	}
}
