using Kernel.Diagnostics;
using Kernel.Drivers.IO;

namespace Kernel.Drivers.Input
{
    /// <summary>
    /// PS/2 Controller driver for handling keyboard and mouse devices
    /// </summary>
    public static unsafe class PS2Controller
    {
        // I/O Port Definitions
        private const ushort DATA_PORT = 0x60;
        private const ushort STATUS_PORT = 0x64;
        private const ushort COMMAND_PORT = 0x64;

        // Controller Commands
        private const byte CMD_READ_CONFIG = 0x20;
        private const byte CMD_WRITE_CONFIG = 0x60;
        private const byte CMD_DISABLE_FIRST_PORT = 0xAD;
        private const byte CMD_ENABLE_FIRST_PORT = 0xAE;
        private const byte CMD_DISABLE_SECOND_PORT = 0xA7;
        private const byte CMD_ENABLE_SECOND_PORT = 0xA8;
        private const byte CMD_TEST_FIRST_PORT = 0xAB;
        private const byte CMD_TEST_SECOND_PORT = 0xA9;
        private const byte CMD_TEST_CONTROLLER = 0xAA;

        // Device Commands
        private const byte DEV_RESET = 0xFF;
        private const byte DEV_IDENTIFY = 0xF2;
        private const byte DEV_ENABLE = 0xF4;
        private const byte DEV_DISABLE = 0xF5;
        private const byte DEV_SET_DEFAULTS = 0xF6;

        // Response Codes
        private const byte RESPONSE_ACK = 0xFA;
        private const byte RESPONSE_SELF_TEST_PASSED = 0x55;
        private const byte RESPONSE_PORT_TEST_PASSED = 0x00;

        // Device Types
        private enum DeviceType
        {
            Unknown = 0,
            Keyboard = 1,
            Mouse = 2,
            ScrollMouse = 3,
            FivebuttonMouse = 4
        }

        // Controller Configuration
        private static bool _dualChannelSupported = false;
        static bool _isInitialized = false;

        /// <summary>
        /// Initializes the PS/2 controller
        /// </summary>
        public static bool Initialize()
        {
            if (_isInitialized) return true;

            SerialDebug.Info("Initializing PS/2 Controller");

            // Disable all PS/2 devices
            SendCommand(CMD_DISABLE_FIRST_PORT);
            SendCommand(CMD_DISABLE_SECOND_PORT);

            // Flush output buffer
            ClearOutputBuffer();

            // Read current configuration
            SendCommand(CMD_READ_CONFIG);
            byte config = ReadData(true);

            // Check dual-channel support
            _dualChannelSupported = (config & 0x20) == 0;

            // Perform controller self-test
            SendCommand(CMD_TEST_CONTROLLER);
            if (ReadData(true) != RESPONSE_SELF_TEST_PASSED)
            {
                SerialDebug.Error("PS/2 Controller self-test failed");
                return false;
            }

            SendCommand(CMD_WRITE_CONFIG);
            SendData(config);

            // Test PS/2 ports
            bool firstPortAvailable = TestPort(CMD_TEST_FIRST_PORT);
            bool secondPortAvailable = _dualChannelSupported && TestPort(CMD_TEST_SECOND_PORT);

            // Configure and enable ports
            config = ConfigurePorts(firstPortAvailable, secondPortAvailable);

            // Initialize available devices
            if (firstPortAvailable)
            {
                InitializeDevice(1);
            }

            if (secondPortAvailable)
            {
                InitializeDevice(2);
            }
            _isInitialized = true;

            SerialDebug.Info("PS/2 Controller initialization complete");
            return true;
        }

        private static bool TestPort(byte testCommand)
        {
            SendCommand(testCommand);
            byte result = ReadData(true);
            return result == RESPONSE_PORT_TEST_PASSED;
        }

        private static byte ConfigurePorts(bool firstPortAvailable, bool secondPortAvailable)
        {
            byte config = 0;

            if (firstPortAvailable)
            {
                config |= 0x01; // Enable first port interrupt
                SendCommand(CMD_ENABLE_FIRST_PORT);
                SerialDebug.Info("First PS/2 port enabled");
            }

            if (secondPortAvailable)
            {
                config |= 0x02; // Enable second port interrupt
                SendCommand(CMD_ENABLE_SECOND_PORT);
                SerialDebug.Info("Second PS/2 port enabled");
            }

            // Write updated configuration
            SendCommand(CMD_WRITE_CONFIG);
            SendData(config);

            return config;
        }

        private static void InitializeDevice(byte port)
        {
            DeviceType deviceType = IdentifyDevice(port);

            switch (deviceType)
            {
                case DeviceType.Keyboard:
                    SerialDebug.Info($"PS/2 Port {port}: Keyboard detected");
                    InitializeKeyboard(port);
                    break;
                case DeviceType.Mouse:
                    SerialDebug.Info($"PS/2 Port {port}: Standard mouse detected");
                    InitializeMouse(port);
                    break;
                default:
                    SerialDebug.Warning($"PS/2 Port {port}: Unknown device");
                    break;
            }
        }

        private static DeviceType IdentifyDevice(byte port)
        {
            if (port == 1)
            {
                SendData(DEV_IDENTIFY);
            }
            else
            {
                SendCommand(0xD4); // Write to second port
                SendData(DEV_IDENTIFY);
            }

            byte ack = ReadData(true);
            if (ack != RESPONSE_ACK)
            {
                return DeviceType.Unknown;
            }

            byte firstByte = ReadData(true);

            if (firstByte == 0x00)
                return DeviceType.Mouse;

            if (firstByte == 0xAB)
            {
                byte secondByte = ReadData(true);
                if (secondByte == 0x83 || secondByte == 0x41 || secondByte == 0xC1)
                    return DeviceType.Keyboard;
            }

            if (firstByte == 0x03)
                return DeviceType.ScrollMouse;

            if (firstByte == 0x04)
                return DeviceType.FivebuttonMouse;

            return DeviceType.Unknown;
        }

        private static void InitializeKeyboard(byte port)
        {
            //ResetDevice(port);
            EnableDevice();
        }

        private static void InitializeMouse(byte port)
        {
            if (port == 2)
                SendCommand(0xD4);

            ResetDevice(port);
            EnableDevice();
        }

        private static void ResetDevice(byte port)
        {
            SendData(DEV_RESET);
            byte response = ReadData(true);
            if (response != RESPONSE_SELF_TEST_PASSED)
            {
                SerialDebug.Error($"Device on port {port} reset failed");
            }
        }

        private static void EnableDevice()
        {
            SendData(DEV_ENABLE);
            byte response = ReadData(true);
            if (response != RESPONSE_ACK)
            {
                SerialDebug.Error("Failed to enable device");
            }
        }

        private static void SendCommand(byte command)
        {
            WaitForInputBuffer();
            IOPort.Out8(COMMAND_PORT, command);
        }

        private static void SendData(byte data)
        {
            WaitForInputBuffer();
            IOPort.Out8(DATA_PORT, data);
        }

        private static byte ReadData(bool logErrors = false)
        {
            for (int attempts = 0; attempts < 1000; attempts++)
            {
                byte status = IOPort.In8(STATUS_PORT);

                if ((status & 0x01) != 0)
                {
                    byte data = IOPort.In8(DATA_PORT);

                    if (logErrors)
                        SerialDebug.Info($"Read data: 0x{((ulong)data).ToStringHex()} after {attempts} attempts");

                    return data;
                }

                Native.Nop();
            }

            if (logErrors)
                SerialDebug.Warning("PS/2 data read timeout");

            return 0;
        }

        private static void ClearOutputBuffer()
        {
            for (int i = 0; i < 32; i++)
            {
                byte status = IOPort.In8(STATUS_PORT);

                if ((status & 0x01) == 0)
                    break;

                IOPort.In8(DATA_PORT);
            }
        }

        private static void WaitForInputBuffer()
        {
            for (int attempts = 0; attempts < 1000; attempts++)
            {
                byte status = IOPort.In8(STATUS_PORT);

                if ((status & 0x02) == 0)
                    return;

                Native.Nop();
            }

            SerialDebug.Warning("PS/2 input buffer wait timeout");
        }
    }
}
