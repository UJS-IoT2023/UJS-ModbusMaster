using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UJS_ModbusMaster.Helpers;
using UJS_ModbusMaster.Services;

namespace UJS_ModbusMaster.ViewModels
{
    public partial class MainViewModel : ObservableObject, IDisposable
    {
        private readonly SerialPortService _serialPortService;
        private readonly ModbusRtuService _modbusService;

        // 串口设置
        [ObservableProperty]
        private string _selectedPort = "COM2";

        [ObservableProperty]
        private int _baudRate = 9600;

        [ObservableProperty]
        private string _parity = "None";

        [ObservableProperty]
        private int _dataBits = 8;

        [ObservableProperty]
        private string _stopBits = "One";

        // 设备设置
        [ObservableProperty]
        private byte _slaveId = 1;

        // 读取设置
        [ObservableProperty]
        private ushort _readStartAddress = 0;

        [ObservableProperty]
        private ushort _readQuantity = 3;

        // 写入设置
        [ObservableProperty]
        private ushort _writeAddress = 3;

        [ObservableProperty]
        private ushort _writeValue = 0xFE;

        // 状态
        [ObservableProperty]
        private string _statusMessage = "就绪";

        [ObservableProperty]
        private bool _isConnected = false;

        // 读取结果
        [ObservableProperty]
        private string _hexResult = "";

        [ObservableProperty]
        private string _uint16Result = "";

        [ObservableProperty]
        private string _floatResult = "";

        [ObservableProperty]
        private string _rawBytesResult = "";

        // 发送和接收的原始数据
        [ObservableProperty]
        private string _sentData = "";

        [ObservableProperty]
        private string _receivedData = "";

        // 可用串口列表
        public ObservableCollection<string> AvailablePorts { get; } = new();

        public ObservableCollection<int> BaudRates { get; } = new()
        {
            9600, 19200, 38400, 57600, 115200
        };

        public ObservableCollection<string> Parities { get; } = new()
        {
            "None", "Even", "Odd", "Mark", "Space"
        };

        public ObservableCollection<int> DataBitsList { get; } = new()
        {
            5, 6, 7, 8
        };

        public ObservableCollection<string> StopBitsList { get; } = new()
        {
            "One", "OnePointFive", "Two"
        };

        public MainViewModel()
        {
            _serialPortService = new SerialPortService();
            _modbusService = new ModbusRtuService(_serialPortService);

            RefreshPorts();
        }

        [RelayCommand]
        private void RefreshPorts()
        {
            AvailablePorts.Clear();
            var ports = SerialPortService.GetPortNames();
            foreach (var port in ports)
            {
                AvailablePorts.Add(port);
            }

            if (!AvailablePorts.Contains(SelectedPort) && AvailablePorts.Count > 0)
            {
                SelectedPort = AvailablePorts[0];
            }
        }

        [RelayCommand]
        private void Connect()
        {
            try
            {
                if (_serialPortService.IsOpen)
                {
                    _serialPortService.Close();
                    IsConnected = false;
                    StatusMessage = "已断开连接";
                    return;
                }

                Parity parity = (Parity)Enum.Parse(typeof(Parity), Parity);
                StopBits stopBits = (StopBits)Enum.Parse(typeof(StopBits), StopBits);

                _serialPortService.Open(SelectedPort, BaudRate, parity, DataBits, stopBits);
                IsConnected = true;
                StatusMessage = $"已连接到 {SelectedPort}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"连接失败: {ex.Message}";
                MessageBox.Show($"连接失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task ReadRegisters()
        {
            if (!_serialPortService.IsOpen)
            {
                StatusMessage = "请先连接串口";
                MessageBox.Show("请先连接串口", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                StatusMessage = $"正在读取从站 {SlaveId} 的寄存器...";

                // 构建请求数据用于显示
                byte[] request = new byte[6];
                request[0] = SlaveId;
                request[1] = 0x03;
                request[2] = (byte)(ReadStartAddress >> 8);
                request[3] = (byte)(ReadStartAddress & 0xFF);
                request[4] = (byte)(ReadQuantity >> 8);
                request[5] = (byte)(ReadQuantity & 0xFF);
                byte[] crc = ModbusCalculator.CalculateCrc(request);

                SentData = BitConverter.ToString(request).Replace("-", " ") + " " + BitConverter.ToString(crc).Replace("-", " ");

                // 执行读取
                byte[] data = await Task.Run(() =>
                    _modbusService.ReadHoldingRegisters(SlaveId, ReadStartAddress, ReadQuantity));

                // 接收到的完整响应（包含从站地址、功能码、字节数、数据和CRC）
                // 构建完整响应帧用于显示
                byte[] fullResponse = new byte[data.Length + 5]; // 1(从站) + 1(功能码) + 1(字节数) + data + 2(CRC)
                fullResponse[0] = SlaveId;
                fullResponse[1] = 0x03;
                fullResponse[2] = (byte)data.Length;
                Array.Copy(data, 0, fullResponse, 3, data.Length);
                byte[] responseCrc = ModbusCalculator.CalculateCrc(fullResponse.Take(fullResponse.Length - 2).ToArray());
                fullResponse[fullResponse.Length - 2] = responseCrc[0];
                fullResponse[fullResponse.Length - 1] = responseCrc[1];

                ReceivedData = BitConverter.ToString(fullResponse).Replace("-", " ");
                RawBytesResult = BitConverter.ToString(data).Replace("-", " ");

                // 转换为不同格式显示
                // 16进制显示
                HexResult = BitConverter.ToString(data).Replace("-", " ");

                // UInt16显示
                ushort[] uint16Values = ModbusRtuService.BytesToUInt16Array(data);
                Uint16Result = string.Join(", ", uint16Values.Select(v => v.ToString()));

                // Float显示（如果数据长度足够）
                if (data.Length >= 4)
                {
                    try
                    {
                        float[] floatValues = ModbusRtuService.BytesToFloatArray(data);
                        FloatResult = string.Join(", ", floatValues.Select(v => v.ToString("F4")));
                    }
                    catch
                    {
                        FloatResult = "无法转换为浮点数";
                    }
                }
                else
                {
                    FloatResult = "数据不足，无法转换为浮点数";
                }

                StatusMessage = $"读取成功: {data.Length} 字节";
            }
            catch (Exception ex)
            {
                StatusMessage = $"读取失败: {ex.Message}";
                MessageBox.Show($"读取失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task WriteRegister()
        {
            if (!_serialPortService.IsOpen)
            {
                StatusMessage = "请先连接串口";
                MessageBox.Show("请先连接串口", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                StatusMessage = $"正在写入从站 {SlaveId} 的寄存器...";

                // 构建请求数据用于显示
                byte[] request = new byte[6];
                request[0] = SlaveId;
                request[1] = 0x06;
                request[2] = (byte)(WriteAddress >> 8);
                request[3] = (byte)(WriteAddress & 0xFF);
                request[4] = (byte)(WriteValue >> 8);
                request[5] = (byte)(WriteValue & 0xFF);
                byte[] crc = ModbusCalculator.CalculateCrc(request);

                SentData = BitConverter.ToString(request).Replace("-", " ") + " " + BitConverter.ToString(crc).Replace("-", " ");

                // 执行写入
                await Task.Run(() =>
                    _modbusService.WriteSingleRegister(SlaveId, WriteAddress, WriteValue));

                // 06功能码的响应与请求相同
                ReceivedData = BitConverter.ToString(request).Replace("-", " ") + " " + BitConverter.ToString(crc).Replace("-", " ");
                StatusMessage = $"写入成功: 地址 {WriteAddress} = {WriteValue} (0x{WriteValue:X4})";
                MessageBox.Show($"成功写入寄存器 {WriteAddress}，值: {WriteValue} (0x{WriteValue:X4})",
                    "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"写入失败: {ex.Message}";
                MessageBox.Show($"写入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ClearResults()
        {
            HexResult = "";
            Uint16Result = "";
            FloatResult = "";
            RawBytesResult = "";
            SentData = "";
            ReceivedData = "";
            StatusMessage = "已清空结果";
        }

        public void Dispose()
        {
            _serialPortService?.Dispose();
        }
    }
}
