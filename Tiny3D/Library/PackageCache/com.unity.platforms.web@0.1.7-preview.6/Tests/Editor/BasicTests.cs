using NUnit.Framework;
using Unity.Platforms.Web;

class BasicTests
{
	[Test]
	public void VerifyCanReferenceWebBuildTarget()
	{
		Assert.IsNotNull(typeof(WebBuildTarget));
	}
}
