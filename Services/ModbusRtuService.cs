using System;
using System.Linq;
using UJS_ModbusMaster.Helpers;

namespace UJS_ModbusMaster.Services
{
    /// <summary>
    /// Modbus RTU 通信服务
    /// </summary>
    public class ModbusRtuService
    {
        private readonly SerialPortService _serialPortService;

        public ModbusRtuService(SerialPortService serialPortService)
        {
            _serialPortService = serialPortService;
        }

        /// <summary>
        /// 读取保持寄存器 (功能码 03)
        /// </summary>
        /// <param name="slaveId">从站地址</param>
        /// <param name="startAddress">起始地址</param>
        /// <param name="quantity">寄存器数量</param>
        /// <returns>读取到的数据</returns>
        public byte[] ReadHoldingRegisters(byte slaveId, ushort startAddress, ushort quantity)
        {
            // 构建请求帧
            // 格式: [从站地址] [功能码] [起始地址高字节] [起始地址低字节] [数量高字节] [数量低字节] [CRC低] [CRC高]
            byte[] request = new byte[6];
            request[0] = slaveId;
            request[1] = 0x03; // 功能码: 读取保持寄存器
            request[2] = (byte)(startAddress >> 8);   // 起始地址高字节
            request[3] = (byte)(startAddress & 0xFF); // 起始地址低字节
            request[4] = (byte)(quantity >> 8);       // 数量高字节
            request[5] = (byte)(quantity & 0xFF);     // 数量低字节

            // 计算CRC
            byte[] crc = ModbusCalculator.CalculateCrc(request);

            // 完整请求帧
            byte[] fullRequest = new byte[request.Length + 2];
            request.CopyTo(fullRequest, 0);
            fullRequest[request.Length] = crc[0]; // CRC低字节
            fullRequest[request.Length + 1] = crc[1]; // CRC高字节

            // 发送请求并接收响应
            // 响应格式: [从站地址] [功能码] [字节数] [数据...] [CRC低] [CRC高]
            int expectedResponseLength = 5 + quantity * 2; // 5 = 从站地址(1) + 功能码(1) + 字节数(1) + CRC(2)
            byte[] response = _serialPortService.SendAndReceive(fullRequest, expectedResponseLength);

            // 验证响应
            if (response.Length < 5)
            {
                throw new Exception("响应数据长度不足");
            }

            // 验证从站地址
            if (response[0] != slaveId)
            {
                throw new Exception($"从站地址不匹配: 期望 {slaveId}, 实际 {response[0]}");
            }

            // 验证功能码
            if (response[1] != 0x03)
            {
                // 异常响应
                if ((response[1] & 0x80) == 0x80)
                {
                    throw new Exception($"Modbus异常: 功能码 {response[1] & 0x7F}, 异常码 {response[2]:X2}");
                }
                throw new Exception($"功能码不匹配: 期望 03, 实际 {response[1]:X2}");
            }

            // 验证CRC
            int dataLength = response[2];
            byte[] responseData = response.Take(response.Length - 2).ToArray();
            byte[] responseCrc = ModbusCalculator.CalculateCrc(responseData);

            if (response[response.Length - 2] != responseCrc[0] || response[response.Length - 1] != responseCrc[1])
            {
                throw new Exception("CRC校验失败");
            }

            // 提取数据
            byte[] result = new byte[dataLength];
            Array.Copy(response, 3, result, 0, dataLength);

            return result;
        }

        /// <summary>
        /// 写单个保持寄存器 (功能码 06)
        /// </summary>
        /// <param name="slaveId">从站地址</param>
        /// <param name="registerAddress">寄存器地址</param>
        /// <param name="value">要写入的值</param>
        public void WriteSingleRegister(byte slaveId, ushort registerAddress, ushort value)
        {
            // 构建请求帧
            // 格式: [从站地址] [功能码] [地址高字节] [地址低字节] [值高字节] [值低字节] [CRC低] [CRC高]
            byte[] request = new byte[6];
            request[0] = slaveId;
            request[1] = 0x06; // 功能码: 写单个寄存器
            request[2] = (byte)(registerAddress >> 8);   // 地址高字节
            request[3] = (byte)(registerAddress & 0xFF); // 地址低字节
            request[4] = (byte)(value >> 8);             // 值高字节
            request[5] = (byte)(value & 0xFF);           // 值低字节

            // 计算CRC
            byte[] crc = ModbusCalculator.CalculateCrc(request);

            // 完整请求帧
            byte[] fullRequest = new byte[request.Length + 2];
            request.CopyTo(fullRequest, 0);
            fullRequest[request.Length] = crc[0];
            fullRequest[request.Length + 1] = crc[1];

            // 发送请求并接收响应
            // 响应格式与请求相同
            byte[] response = _serialPortService.SendAndReceive(fullRequest, 8);

            // 验证响应
            if (response.Length != 8)
            {
                throw new Exception($"响应数据长度错误: 期望 8, 实际 {response.Length}");
            }

            // 验证从站地址
            if (response[0] != slaveId)
            {
                throw new Exception($"从站地址不匹配: 期望 {slaveId}, 实际 {response[0]}");
            }

            // 验证功能码
            if (response[1] != 0x06)
            {
                if ((response[1] & 0x80) == 0x80)
                {
                    throw new Exception($"Modbus异常: 功能码 {response[1] & 0x7F}, 异常码 {response[2]:X2}");
                }
                throw new Exception($"功能码不匹配: 期望 06, 实际 {response[1]:X2}");
            }

            // 验证CRC
            byte[] responseData = response.Take(6).ToArray();
            byte[] responseCrc = ModbusCalculator.CalculateCrc(responseData);

            if (response[6] != responseCrc[0] || response[7] != responseCrc[1])
            {
                throw new Exception("CRC校验失败");
            }

            // 验证写入的地址和值
            ushort responseAddress = (ushort)((response[2] << 8) | response[3]);
            ushort responseValue = (ushort)((response[4] << 8) | response[5]);

            if (responseAddress != registerAddress)
            {
                throw new Exception($"寄存器地址不匹配: 期望 {registerAddress}, 实际 {responseAddress}");
            }

            if (responseValue != value)
            {
                throw new Exception($"写入值不匹配: 期望 {value}, 实际 {responseValue}");
            }
        }

        /// <summary>
        /// 将字节数组转换为16位无符号整数数组（大端序）
        /// </summary>
        public static ushort[] BytesToUInt16Array(byte[] data)
        {
            if (data.Length % 2 != 0)
            {
                throw new ArgumentException("数据长度必须是2的倍数");
            }

            ushort[] result = new ushort[data.Length / 2];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (ushort)((data[i * 2] << 8) | data[i * 2 + 1]);
            }
            return result;
        }

        /// <summary>
        /// 将字节数组转换为浮点数数组（32位，大端序）
        /// </summary>
        public static float[] BytesToFloatArray(byte[] data)
        {
            if (data.Length % 4 != 0)
            {
                throw new ArgumentException("数据长度必须是4的倍数");
            }

            float[] result = new float[data.Length / 4];
            for (int i = 0; i < result.Length; i++)
            {
                byte[] floatBytes = new byte[4];
                // Modbus大端序: 高字节在前
                floatBytes[0] = data[i * 4 + 2]; // 低字低字节
                floatBytes[1] = data[i * 4 + 3]; // 低字高字节
                floatBytes[2] = data[i * 4 + 0]; // 高字低字节
                floatBytes[3] = data[i * 4 + 1]; // 高字高字节
                result[i] = BitConverter.ToSingle(floatBytes, 0);
            }
            return result;
        }

        /// <summary>
        /// 将浮点数转换为字节数组（32位，Modbus大端序）
        /// </summary>
        public static byte[] FloatToBytes(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            // 转换为Modbus大端序
            return new byte[] { bytes[3], bytes[2], bytes[1], bytes[0] };
        }

        /// <summary>
        /// 将16位无符号整数转换为字节数组（大端序）
        /// </summary>
        public static byte[] UInt16ToBytes(ushort value)
        {
            return new byte[] { (byte)(value >> 8), (byte)(value & 0xFF) };
        }
    }
}
