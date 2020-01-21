using NUnit.Framework;
using Unity.Platforms.Linux;

class BasicTests
{
	[Test]
	public void VerifyCanReferenceLinuxBuildTarget()
	{
		Assert.IsNotNull(typeof(LinuxBuildTarget));
	}
}
