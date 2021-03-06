//
// NetworkInterfaceTest.cs - NUnit Test Cases for System.Net.NetworkInformation.NetworkInterface
//
// Authors:
//   Ben Woods (woodsb02@gmail.com)
//

using NUnit.Framework;
using System;
using System.Net;
using System.Net.NetworkInformation;

namespace MonoTests.System.Net.NetworkInformation
{

	[TestFixture]
	public class NetworkInterfaceTest
	{
		[Test]
		public void IsNetworkAvailable ()
		{
			Assert.IsTrue (NetworkInterface.GetIsNetworkAvailable ());
		}
	
		[Test]
		public void LoopbackInterfaceIndex ()
		{
			Assert.Greater (NetworkInterface.LoopbackInterfaceIndex, 0);
		}
	
		[Test]
		public void AtLeastOneInterface ()
		{
			NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces ();
			Assert.Greater (adapters.Length, 0);
		}
	
		[Test]
		public void FirstInterfaceId ()
		{
			NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces ();
			Assert.Greater (adapters[0].Id.Length, 0);
		}
	
		[Test]
		public void FirstInterfaceName ()
		{
			NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces ();
			Assert.Greater (adapters[0].Name.Length, 0);
		}
	
		[Test]
		public void FirstInterfaceType ()
		{
			NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces ();
			Assert.AreNotEqual (adapters[0].NetworkInterfaceType, NetworkInterfaceType.Unknown);
		}
	
		[Test]
		public void FirstInterfaceOperationalStatus ()
		{
			NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces ();
			Assert.AreNotEqual (adapters[0].OperationalStatus, OperationalStatus.Unknown);
		}
	
		[Test]
		public void FirstInterfaceSpeed ()
		{
			NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces ();
			Assert.Greater (adapters[0].Speed, 0);
		}
	
	}
}
