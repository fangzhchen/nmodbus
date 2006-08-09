using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.IO.Ports;
using Modbus.Device;

namespace Modbus.IntegrationTests
{
	[TestFixture]
	public class ModbusASCIIMasterFixture : ModbusMasterFixture
	{
		[TestFixtureSetUp]
		public void Init()
		{
			Master = new ModbusASCIIMaster(Port);			
		}
	}
}