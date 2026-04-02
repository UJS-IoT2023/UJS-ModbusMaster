using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UJS_ModbusMaster.Helpers
{
    public static class ModbusCalculator
    {
        /// <summary>
        /// 计算 Modbus RTU 标准的 CRC16 校验码
        /// </summary>
        public static byte[] CalculateCrc(byte[] buffer)
        {
            ushort crc = 0xFFFF;
            for (int i = 0; i < buffer.Length; i++)
            {
                crc ^= buffer[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }
            // Modbus 规定低位在前，高位在后
            return new byte[] { (byte)(crc & 0xFF), (byte)(crc >> 8) };
        }
    }
}
