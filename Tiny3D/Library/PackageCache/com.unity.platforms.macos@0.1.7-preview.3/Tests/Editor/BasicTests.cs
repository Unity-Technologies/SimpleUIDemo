using NUnit.Framework;
using Unity.Platforms.MacOS;

class BasicTests
{
	[Test]
	public void VerifyCanReferenceMacOSBuildTarget()
	{
		Assert.IsNotNull(typeof(MacOSBuildTarget));
	}
}
