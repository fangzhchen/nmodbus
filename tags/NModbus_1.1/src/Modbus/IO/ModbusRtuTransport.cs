using System;
using System.Collections.Generic;
using log4net;
using Modbus.Message;
using Modbus.Util;

namespace Modbus.IO
{
	class ModbusRtuTransport : ModbusSerialTransport
	{		
		public const int RequestFrameStartLength = 7;
		public const int ResponseFrameStartLength = 4;

		private static readonly ILog _log = LogManager.GetLogger(typeof(ModbusRtuTransport));

		public ModbusRtuTransport ()
		{
		}

		public ModbusRtuTransport(SerialPortAdapter serialPortStreamAdapter)
			: base(serialPortStreamAdapter)
		{
		}

		internal override byte[] BuildMessageFrame(IModbusMessage message)
		{
			List<byte> messageBody = new List<byte>();
			messageBody.Add(message.SlaveAddress);
			messageBody.AddRange(message.ProtocolDataUnit);
			messageBody.AddRange(ModbusUtil.CalculateCrc(message.MessageFrame));

			return messageBody.ToArray();
		}

		internal override bool ChecksumsMatch(IModbusMessage message, byte[] messageFrame)
		{
			return BitConverter.ToUInt16(messageFrame, messageFrame.Length - 2) == BitConverter.ToUInt16(ModbusUtil.CalculateCrc(message.MessageFrame), 0);
		}

		internal override T ReadResponse<T>()
		{
			byte[] frameStart = Read(ResponseFrameStartLength);
			byte[] frameEnd = Read(ResponseBytesToRead(frameStart));
			byte[] frame = CollectionUtil.Combine<byte>(frameStart, frameEnd);
			_log.InfoFormat("RX: {0}", StringUtil.Join(", ", frame));

			return CreateResponse<T>(frame);
		}

		internal override byte[] ReadRequest()
		{			
			byte[] frameStart = Read(RequestFrameStartLength);
			byte[] frameEnd = Read(RequestBytesToRead(frameStart));
			byte[] frame = CollectionUtil.Combine<byte>(frameStart, frameEnd);
			_log.InfoFormat("RX: {0}", StringUtil.Join(", ", frame));

			return frame;
		}

		public virtual byte[] Read(int count)
		{
			byte[] frameBytes = new byte[count];
			int numBytesRead = 0;			

			while (numBytesRead != count)
				numBytesRead += _serialPortStreamAdapter.Read(frameBytes, numBytesRead, count - numBytesRead);			

			return frameBytes;
		}

		public static int RequestBytesToRead(byte[] frameStart)
		{
			byte functionCode = frameStart[1];
			int numBytes;

			switch (functionCode)
			{
				case Modbus.ReadCoils:
				case Modbus.ReadInputs:
				case Modbus.ReadHoldingRegisters:
				case Modbus.ReadInputRegisters:
				case Modbus.WriteSingleCoil:
				case Modbus.WriteSingleRegister:
				case Modbus.Diagnostics:
					numBytes = 1;
					break;
				case Modbus.WriteMultipleCoils:
				case Modbus.WriteMultipleRegisters:
					byte byteCount = frameStart[6];
					numBytes = byteCount + 2;
					break;
				default:
					string errorMessage = String.Format("Function code {0} not supported.", functionCode);
					_log.Error(errorMessage);
					throw new NotImplementedException(errorMessage);
			}

			return numBytes;
		}

		public static int ResponseBytesToRead(byte[] frameStart)
		{
			byte functionCode = frameStart[1];
			byte byteCount = frameStart[2];
			int numBytes;

			switch (functionCode)
			{
				case Modbus.ReadCoils:
				case Modbus.ReadInputs:
				case Modbus.ReadHoldingRegisters:
				case Modbus.ReadInputRegisters:
					numBytes = byteCount + 1;
					break;
				case Modbus.WriteSingleCoil:
				case Modbus.WriteSingleRegister:
				case Modbus.WriteMultipleCoils:
				case Modbus.WriteMultipleRegisters:
				case Modbus.Diagnostics:
					numBytes = 4;
					break;
				default:
					string errorMessage = String.Format("Function code {0} not supported.", functionCode);
					_log.Error(errorMessage);
					throw new NotImplementedException(errorMessage);
			}

			return numBytes;
		}	
	}
}
