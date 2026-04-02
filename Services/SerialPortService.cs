using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace UJS_ModbusMaster.Services
{
    /// <summary>
    /// 串口通信服务
    /// </summary>
    public class SerialPortService : IDisposable
    {
        private SerialPort _serialPort;
        private readonly object _lock = new object();

        /// <summary>
        /// 串口是否已打开
        /// </summary>
        public bool IsOpen => _serialPort?.IsOpen ?? false;

        /// <summary>
        /// 打开串口
        /// </summary>
        public void Open(string portName, int baudRate = 9600, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            Close();

            _serialPort = new SerialPort
            {
                PortName = portName,
                BaudRate = baudRate,
                Parity = parity,
                DataBits = dataBits,
                StopBits = stopBits,
                ReadTimeout = 1000,
                WriteTimeout = 1000
            };

            _serialPort.Open();
        }

        /// <summary>
        /// 关闭串口
        /// </summary>
        public void Close()
        {
            if (_serialPort != null)
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
                _serialPort.Dispose();
                _serialPort = null;
            }
        }

        /// <summary>
        /// 发送数据并接收响应
        /// </summary>
        public byte[] SendAndReceive(byte[] data, int expectedLength = 0)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                throw new InvalidOperationException("串口未打开");
            }

            lock (_lock)
            {
                // 清空缓冲区
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();

                // 发送数据
                _serialPort.Write(data, 0, data.Length);

                // 等待响应
                Thread.Sleep(100);

                // 读取响应
                var response = new System.Collections.Generic.List<byte>();
                DateTime startTime = DateTime.Now;

                while ((DateTime.Now - startTime).TotalMilliseconds < 1000)
                {
                    if (_serialPort.BytesToRead > 0)
                    {
                        byte[] buffer = new byte[_serialPort.BytesToRead];
                        int read = _serialPort.Read(buffer, 0, buffer.Length);
                        response.AddRange(buffer[..read]);

                        // 如果已经读取到足够的数据，退出
                        if (expectedLength > 0 && response.Count >= expectedLength)
                        {
                            break;
                        }

                        // 如果3.5个字符时间没有新数据，认为接收完成
                        Thread.Sleep(4);
                        if (_serialPort.BytesToRead == 0)
                        {
                            break;
                        }
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }

                return response.ToArray();
            }
        }

        /// <summary>
        /// 获取可用串口列表
        /// </summary>
        public static string[] GetPortNames()
        {
            return SerialPort.GetPortNames();
        }

        public void Dispose()
        {
            Close();
        }
    }
}
