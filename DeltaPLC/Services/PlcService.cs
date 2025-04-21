using System.IO.Ports;

namespace DeltaPLC.Services
{
    public class PlcService
    {
        private SerialPort _serialPort;
        private bool _isConnected = false;
        public string Connect(string portName)
        {
            try
            {
                _serialPort = new SerialPort(portName, 9600, Parity.Even, 7, StopBits.One)
                {
                    ReadTimeout = 2000,
                    WriteTimeout = 2000
                };

                _serialPort.Open();
                _isConnected = true;

                return $"✅ Connected to PLC on {portName}";
            }
            catch (Exception ex)
            {
                return $"❌ Connection Failed: {ex.Message}";
            }
        }
        public string Disconnect()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
                _isConnected = false;
                return "🔌 Disconnected from PLC";
            }

            return "⚠ Already Disconnected";
        }
        public async Task<int> ReadRegisterAsync(int register)
        {
            if (!_isConnected) throw new InvalidOperationException("❌ Not connected to PLC.");

            try
            {
                register += 4096;
                string regNum = register.ToString("X4");

                string request = CreateAsciiRequest(1, 3, register, 1);
                Console.WriteLine($"🔹 Sending Read Request: {request}");

                _serialPort.WriteLine(request);

                await Task.Delay(500);

                string response = await Task.Run(() => _serialPort.ReadLine()?.Trim()
                    ?? throw new Exception("❌ No response from PLC"));
                Console.WriteLine($"Raw Response: [{response}]");

                return ParseModbusResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Read Error: {ex.Message}");
                return -1;
            }
        }
        private int ParseModbusResponse(string response)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(response) || response.Length < 11)
                    throw new Exception("❌ Invalid or too short response from PLC.");

                if (!response.StartsWith(":"))
                    throw new Exception("❌ Invalid Modbus ASCII response format.");

                if (!ValidateLRC(response))
                    throw new Exception("❌ LRC Checksum mismatch!");

                string byteCountHex = response.Substring(5, 2);
                int byteCount = Convert.ToInt32(byteCountHex, 16);

                if (byteCount * 2 + 9 != response.Length)
                    throw new Exception($"❌ Byte count mismatch! Expected: {byteCount}, Found: {(response.Length - 9) / 2}");

                string dataHex = response.Substring(7, byteCount * 2);
                Console.WriteLine($"🔍 Extracted HEX Data: {dataHex}");

                int decimalValue = Convert.ToInt32(dataHex, 16);
                Console.WriteLine($"✅ Converted Decimal Value: {decimalValue}");

                return decimalValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error Parsing Response: {ex.Message}");
                return -1;
            }
        }
        private bool ValidateLRC(string response)
        {
            string dataWithoutLRC = response.Substring(1, response.Length - 3);
            string lrcHex = response.Substring(response.Length - 2, 2);

            int computedLRC = ComputeLRC(dataWithoutLRC);
            int receivedLRC = Convert.ToInt32(lrcHex, 16);

            return computedLRC == receivedLRC;
        }
        private int ComputeLRC(string data)
        {
            byte lrc = 0;
            for (int i = 0; i < data.Length; i += 2)
            {
                lrc += Convert.ToByte(data.Substring(i, 2), 16);
            }
            return ((lrc ^ 0xFF) + 1) & 0xFF;
        }
        public async Task<string> WriteRegisterAsync(int register, int value)
        {
            if (!_isConnected) return "❌ Not connected to PLC.";

            try
            {
                register += 4096;
                string request = CreateAsciiRequest(1, 6, register, value);

                Console.WriteLine($"🔹 Sending Write Request: {request}");
                _serialPort.WriteLine(request);

                await Task.Delay(500);

                string response = await Task.Run(() => _serialPort.ReadLine()?.Trim() ?? "❌ No response from PLC");
                Console.WriteLine($"🔍 Write Response: [{response}]");

                return string.IsNullOrEmpty(response) ? "No response received from the PLC." : "Write successful! ";

            }
            catch (Exception ex)
            {
                return $"❌ Error: {ex.Message}";
            }
        }
        private string CreateAsciiRequest(byte slave, byte function, int address, int value)
        {
            string addressHex = address.ToString("X4");
            string valueHex = value.ToString("X4");
            string command = $"{slave:X2}{function:X2}{addressHex}{valueHex}";

            string lrc = CalculateLRC(command);
            return $":{command}{lrc}\r\n";
        }
        private string CalculateLRC(string command)
        {
            byte lrc = 0;
            for (int i = 0; i < command.Length; i += 2)
            {
                lrc += Convert.ToByte(command.Substring(i, 2), 16);
            }
            lrc = (byte)((~lrc + 1) & 0xFF);
            return lrc.ToString("X2");
        }
    }
}
