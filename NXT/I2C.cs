using System;
using System.Threading;
using System.Diagnostics;
namespace MonoBrick.NXT
{

    #region Base I2C Sensor
    /// <summary>
    /// Sensor mode when using a temperature sensor
    /// </summary>
    public enum I2CMode
    {
        LowSpeed = SensorType.LowSpeed, LowSpeed9V = SensorType.LowSpeed9V
    };

    /// <summary>
    /// Abstract class to use for I2C sensors
    /// </summary>
    public abstract class I2CBase : Sensor
    {
        private readonly int pollTime;
        private const int I2CTimeOut = 500;//in MS

        /// <summary>
        /// Initializes a new instance of the <see cref="I2CSensor"/> class.
        /// </summary>
        /// <param name='mode'>
        /// I2C mode
        /// </param>
        /// <param name='sensorAddress'>
        /// I2C address.
        /// </param>
        public I2CBase(I2CMode mode, byte sensorAddress) : base((SensorType)mode, SensorMode.Raw)
        {
            I2CAddress = sensorAddress;
            pollTime = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="I2CSensor"/> class.
        /// </summary>
        /// <param name='mode'>
        /// I2C mode
        /// </param>
        /// <param name='sensorAddress'>
        /// I2C address.
        /// </param>
        /// <param name='pollInterval'>
        /// Poll interval between checking for new values. This may need some tweaking depending on the sensor
        /// </param>
        public I2CBase(I2CMode mode, byte sensorAddress, int pollInterval) : base((SensorType)mode, SensorMode.Raw)
        {
            I2CAddress = sensorAddress;
            pollTime = pollInterval;
        }

        /// <summary>
        /// Gets the i2C address 
        /// </summary>
        /// <value>
        /// The i2C address.
        /// </value>
        public byte I2CAddress { get; }

        /// <summary>
        /// Initialize the sensor
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            try
            {
                ReadRegister(0x00, 1);
            }
            catch
            {

            }
            ReadRegister(0x00, 1);//make sure exception is thrown if read fails  

        }

        /// <summary>
        /// Reads a 8 byte register from the sensor
        /// </summary>
        /// <returns>
        /// The bytes that was read
        /// </returns>
        /// <param name='register'>
        /// Register to read
        /// </param>
        protected byte[] ReadRegister(byte register)
        {
            return ReadRegister(register, 8);
        }

        /// <summary>
        /// Reads a register from the sensor
        /// </summary>
        /// <returns>
        /// The bytes that was read
        /// </returns>
        /// <param name='register'>
        /// Register to read
        /// </param>
        /// <param name='rxLength'>
        /// The number of bytes to read
        /// </param>
        protected byte[] ReadRegister(byte register, byte rxLength)
        {
            if (!hasInit)
            {
                Initialize();
            }
            byte[] command = { I2CAddress, register };
            return I2CWriteAndRead(command, rxLength);
        }

        /// <summary>
        /// Writes a byte to a register.
        /// </summary>
        /// <param name='register'>
        /// Register to write to
        /// </param>
        /// <param name='data'>
        /// Data byte to write
        /// </param>
        protected void WriteRegister(byte register, byte data)
        {
            WriteRegister(register, data, false);
        }

        /// <summary>
        /// Writes a byte to a register.
        /// </summary>
        /// <param name='register'>
        /// Register to write to
        /// </param>
        /// <param name='data'>
        /// Data byte to write
        /// </param>
        /// <param name='reply'>
        /// If set to <c>true</c> brick will send a reply
        /// </param>
        protected void WriteRegister(byte register, byte data, bool reply)
        {
            if (!hasInit)
            {
                Initialize();
            }
            byte[] command = { I2CAddress, register, data };
            I2CWrite(command, 0, reply);
        }

        /// <summary>
        /// Reads the sensor value as a string.
        /// </summary>
        /// <returns>
        /// The value as a string
        /// </returns>
        public override abstract string ReadAsString();//From sensor class made abstract

        /// <summary>
        /// Reset the sensor
        /// </summary>
        /// <param name='reply'>
        /// If set to <c>true</c> the brick will send a reply
        /// </param>
		public override void Reset(bool reply)
        {
            UpdateTypeAndMode(Type, Mode);
            Initialize();
        }

        /// <summary>
        /// Write byte array to sensor
        /// </summary>
        /// <param name='txData'>
        /// The byte array to write
        /// </param>
        /// <param name='rxLength'>
        /// The length of the expected reply
        /// </param>
        protected void I2CWrite(byte[] txData, byte rxLength)
        {
            I2CWrite(txData, rxLength, false);
        }

        /// <summary>
        /// Write byte array to sensor
        /// </summary>
        /// <param name='txData'>
        /// The byte array to write
        /// </param>
        /// <param name='rxLength'>
        /// The length of the expected reply
        /// </param>
        /// <param name='reply'>
        /// If set to <c>true</c> brick will send a reply
        /// </param>
        protected void I2CWrite(byte[] txData, byte rxLength, bool reply)
        {
            var command = new Command(CommandType.DirectCommand, CommandByte.LsWrite, reply);
            command.Append((byte)Port);
            command.Append((byte)txData.Length);
            command.Append(rxLength);
            command.Append(txData);
            connection.Send(command);
            if (reply)
            {
                var brickReply = connection.Receive();
                Error.CheckForError(brickReply, 5);
            }
        }

        private void I2CWaitForBytes(byte numberOfBytes)
        {
            Stopwatch stopWatch = new Stopwatch();
            byte bytesRead = 0;
            stopWatch.Start();
            while (bytesRead != numberOfBytes && stopWatch.ElapsedMilliseconds < I2CTimeOut)
            {
                try
                {
                    bytesRead = BytesReady();
                }
                catch (MonoBrickException e)
                {
                    if (e.ErrorCode != (byte)BrickError.PendingCommunication && e.ErrorCode != (byte)BrickError.CommunicationBusError)
                    {
                        Error.ThrowException(e.ErrorCode);
                    }
                }
                Thread.Sleep(pollTime);
            }
            if (stopWatch.ElapsedMilliseconds > I2CTimeOut)
            {
                throw new BrickException(BrickError.I2CTimeOut);
            }
        }

        private byte[] I2CRead()
        {
            var command = new Command(CommandType.DirectCommand, CommandByte.LsRead, true);
            command.Append((byte)Port);
            var reply = connection.SendAndReceive(command);
            Error.CheckForError(reply, 20);
            byte size = reply[3];
            byte[] data = reply.GetData(4);
            Array.Resize(ref data, size);
            return data;
        }

        /// <summary>
        /// Write and read an array of bytes to sensor
        /// </summary>
        /// <returns>
        /// The bytes that was read
        /// </returns>
        /// <param name='data'>
        /// Byte array to write
        /// </param>
        /// <param name='rxLength'>
        /// Length of the expected reply
        /// </param>
        protected byte[] I2CWriteAndRead(byte[] data, byte rxLength)
        {
            I2CWrite(data, rxLength);
            //Thread.Sleep(60);
            I2CWaitForBytes(rxLength);
            return I2CRead();
        }

        private byte BytesReady()
        {
            var command = new Command(CommandType.DirectCommand, CommandByte.LsGetStatus, true);
            command.Append((byte)port);
            var reply = connection.SendAndReceive(command);
            Error.CheckForError(reply, 4);
            return reply[3];
        }
    }
    #endregion

    #region I2C senor

    /// <summary>
    /// I2C sensor class for reading and writing to a I2C sensor
    /// </summary>
    public class I2CSensor : I2CBase
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="I2CSensor"/> class with I2C address 0x02
        /// </summary>
        public I2CSensor() : base(I2CMode.LowSpeed9V, 0x02)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="I2CSensor"/> class.
        /// </summary>
        /// <param name='mode'>
        /// 9v or normal mode
        /// </param>
        /// <param name='sensorAddress'>
        /// Sensor I2C address
        /// </param>
        public I2CSensor(I2CMode mode, byte sensorAddress) : base(mode, sensorAddress)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonoBrick.NXT.I2CSensor"/> class.
        /// </summary>
        /// <param name='mode'>
        /// 9v or normal mode
        /// </param>
        /// <param name='sensorAddress'>
        /// Sensor I2C address.
        /// </param>
        /// <param name='pollInterval'>
        /// Poll interval between checking for new values. This may need some tweaking depending on the sensor
        /// </param>
        public I2CSensor(I2CMode mode, byte sensorAddress, int pollInterval) : base(mode, sensorAddress, pollInterval)
        {

        }

        /// <summary>
        /// Reads 8bytes from the sensor register.
        /// </summary>
        /// <returns>
        /// The bytes read from the register
        /// </returns>
        /// <param name='register'>
        /// Register address to start reading from
        /// </param>
        new public byte[] ReadRegister(byte register)
        {
            return base.ReadRegister(register, 8);
        }

        /// <summary>
        /// Reads x bytes from the sensor register.
        /// </summary>
        /// <returns>
        /// The bytes read from the register
        /// </returns>
        /// <param name='register'>
        /// Register address to start reading from
        /// </param>
        /// <param name='rxLength'>
        /// Number of bytes to to read from the register
        /// </param>
        new public byte[] ReadRegister(byte register, byte rxLength)
        {
            return base.ReadRegister(register, rxLength);
        }

        /// <summary>
        /// Writes to the sensor register.
        /// </summary>
        /// <param name='register'>
        /// Register to write to 
        /// </param>
        /// <param name='data'>
        /// Data byte to write
        /// </param>
        new public void WriteRegister(byte register, byte data)
        {
            base.WriteRegister(register, data);
        }

        /// <summary>
        /// Writes to the sensor register.
        /// </summary>
        /// <param name='register'>
        /// Register to write to 
        /// </param>
        /// <param name='data'>
        /// Data byte to write
        /// </param>
        /// <param name='reply'>
        /// If set to <c>true</c> reply from the brick will be send
        /// </param>
        new public void WriteRegister(byte register, byte data, bool reply)
        {
            base.WriteRegister(register, data, reply);
        }

        /// <summary>
        /// Is not implemented
        /// </summary>
        /// <returns>
        /// An empty string
        /// </returns>
        public override string ReadAsString() { return ""; }
    }
    #endregion

    #region Sonar Sensor
    /// <summary>
    /// Sensor mode when using a Sonar sensor
    /// </summary>
    public enum SonarMode
    {
        /// <summary>
        /// Result will be in centimeter
        /// </summary>
        Centimeter = 1,
        /// <summary>
        /// Result will be in centi-inch
        /// </summary>
        CentiInch = 2
    };

    internal enum SonarCommand : byte { Off = 00, SingleShot = 0x01, Continuous = 0x02, EventCapture = 0x03, RequestWarmReset = 0x04 };
    internal enum SonarRegister : byte
    {
        Version = 0x00, ProductId = 0x08, SensorType = 0x10, FactoryZeroValue = 0x11, FactoryScaleFactor = 0x12,
        FactoryScaleDivisor = 0x13, MeasurementUnits = 0x14,
        Interval = 0x40, Command = 0x41, Result1 = 0x42, Result2 = 0x43, Result3 = 0x44, Result4 = 0x45, Result5 = 0x46,
        Result6 = 0x47, Result7 = 0x48, Result8 = 0x49, ZeroValue = 0x50, ScaleFactor = 0x51, ScaleDivisor = 0x52,
    };

    internal class SonarSettings
    {
        private readonly byte scaleDivision;
        public SonarSettings(byte zero, byte scaleFactor, byte scaleDivision)
        {
            Zero = zero;
            ScaleFactor = scaleFactor;
            this.scaleDivision = scaleDivision;
        }
        public SonarSettings(byte[] data)
        {
            if (data.Length == 3)
            {
                Zero = data[0];
                ScaleFactor = data[1];
                scaleDivision = data[2];
            }
            else
            {
                Zero = 0;
                ScaleFactor = 0;
                scaleDivision = 0;
            }
        }
        public byte Zero { get; }
        public byte ScaleFactor { get; }
        public byte ScaleDivision => ScaleFactor;
        public override string ToString()
        {
            return "Zero: " + Zero.ToString() + " Scale factor: " + ScaleFactor.ToString() + " Scale division: " + scaleDivision.ToString();
        }
    }

    /// <summary>
    /// Sonar sensor
    /// </summary>
    public class Sonar : I2CBase
    {
        private const byte SonarAddress = 0x02;

        /// <summary>
        /// Gets or sets the sonar mode.
        /// </summary>
        /// <value>
        /// The sonar mode 
        /// </value>
        public new SonarMode Mode { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sonar"/> class in centimeter mode
        /// </summary>
        public Sonar() : base(I2CMode.LowSpeed, SonarAddress) { Mode = SonarMode.Centimeter; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sonar"/> class.
        /// </summary>
        /// <param name='mode'>
        /// The sonar mode
        /// </param>
        public Sonar(SonarMode mode) : base(I2CMode.LowSpeed, SonarAddress) { Mode = mode; }

        /// <summary>
        /// Initialize the sensor
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            //Console.WriteLine("Check for single shot");
        }

        /// <summary>
        /// Read the distance in either centiinches or centimeter
        /// </summary>
        public int ReadDistance()
        {
            int reading = ReadRegister((byte)SonarRegister.Result1, 1)[0];
            if (Mode == SonarMode.CentiInch)
                return reading * 39370 / 1000;
            return reading;
        }
        /// <summary>
        /// Fire a single shot 
        /// </summary>
        /// <param name='reply'>
        /// If set to <c>true</c> the brick will send a reply
        /// </param>
        public void SingleShot(bool reply)
        {
            SetMode(SonarCommand.SingleShot, reply);
        }

        /// <summary>
        /// Turn off the sonar to save power
        /// </summary>
        /// <param name='reply'>
        /// If set to <c>true</c> brick will send a reply
        /// </param>
        public void Off(bool reply)
        {
            SetMode(SonarCommand.Off, reply);
        }

        /// <summary>
        /// Do Continuous measurements
        /// </summary>
        /// <param name='reply'>
        /// If set to <c>true</c> brick will send a reply
        /// </param>
        public void Continuous(bool reply)
        {
            SetMode(SonarCommand.Continuous, reply);
        }

        /// <summary>
        /// Determines whether sonar is off.
        /// </summary>
        /// <returns>
        /// <c>true</c> if sonar is off; otherwise, <c>false</c>.
        /// </returns>
        bool IsOff()
        {
            if (GetMode() == SonarCommand.Off)
                return true;
            return false;
        }

        /// <summary>
        /// Reset the sensor
        /// </summary>
        /// <param name='reply'>
        /// If set to <c>true</c> brick will send a reply
        /// </param>
        public override void Reset(bool reply)
        {
            SetMode(SonarCommand.RequestWarmReset, reply);
        }

        private byte GetContinuousInterval()
        {
            return ReadRegister((byte)SonarRegister.Interval, 1)[0];
        }

        private void SetContinuousInterval(byte interval)
        {
            SetContinuousInterval(interval, false);
        }

        private void SetContinuousInterval(byte interval, bool reply)
        {
            WriteRegister((byte)SonarRegister.Interval, interval, reply);
            Thread.Sleep(60);
        }

        private SonarSettings GetFactorySettings()
        {
            return new SonarSettings(ReadRegister((byte)SonarRegister.FactoryZeroValue, 1)[0],
                                        ReadRegister((byte)SonarRegister.FactoryScaleFactor, 1)[0],
                                        ReadRegister((byte)SonarRegister.FactoryScaleDivisor, 1)[0]);
        }

        private SonarSettings GetActualSettings()
        {
            return new SonarSettings(ReadRegister((byte)SonarRegister.ZeroValue, 1)[0],
                                        ReadRegister((byte)SonarRegister.ScaleFactor, 1)[0],
                                        ReadRegister((byte)SonarRegister.ScaleDivisor, 1)[0]);
        }

        private SonarCommand GetMode()
        {
            return (SonarCommand)ReadRegister((byte)SonarRegister.Command, 1)[0];
        }

        private void SetMode(SonarCommand command)
        {
            SetMode(command, false);
        }

        private void SetMode(SonarCommand command, bool reply)
        {
            WriteRegister((byte)SonarRegister.Command, (byte)command, reply);
            Thread.Sleep(60);
        }

        /// <summary>
        /// Reads the sensor value as a string.
        /// </summary>
        /// <returns>
        /// The value as a string
        /// </returns>
		public override string ReadAsString()
        {
            string s = ReadDistance().ToString();
            if (Mode == SonarMode.CentiInch)
                s += " centi-inches";
            else
                s += " centimeters";
            return s;
        }

    }
    #endregion //Sonar Sensor

    #region HiTechnic color sensor
    internal enum ColorRegister : byte
    {
        Version = 0x00, ProductId = 0x08, SensorType = 0x10, Command = 0x41, ColorNumber = 0x42, RedReading = 0x43,
        GreenReading = 0x44, BlueReading = 0x45, RedRawReadingLow = 0x46, RedRawReadingHigh = 0x47, GreenRawReadingLow = 0x48, GreenRawReadingHigh = 0x49,
        BlueRawReadingLow = 0x4A, BlueRawReadingHigh = 0x4B, ColorIndexNo = 0x4c, RedNormalized = 0x4d, GreenNormalized = 0x4e, BlueNormalized = 0x4f
    };

    /// <summary>
    /// Class that holds RGB colors
    /// </summary>
    public class RGBColor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RGBColor"/> class.
        /// </summary>
        /// <param name='red'>
        /// Red value
        /// </param>
        /// <param name='green'>
        /// Green value
        /// </param>
        /// <param name='blue'>
        /// Blue value
        /// </param>
        public RGBColor(byte red, byte green, byte blue) { Red = red; Green = green; Blue = blue; }

        /// <summary>
        /// Gets the red value
        /// </summary>
        /// <value>
        /// The red value
        /// </value>
        public byte Red { get; }

        /// <summary>
        /// Gets the green value
        /// </summary>
        /// <value>
        /// The green value
        /// </value>
        public byte Green { get; }

        /// <summary>
        /// Gets the blue value
        /// </summary>
        /// <value>
        /// The blue value
        /// </value>
        public byte Blue { get; }
    }

    /// <summary>
    /// HiTechnic color sensor
    /// </summary>
    public class HiTecColor : I2CBase
    {
        private const byte ColorAddress = 0x02;

        /// <summary>
        /// Initializes a new instance of the <see cref="HiTecColor"/> class.
        /// </summary>
        public HiTecColor() : base(I2CMode.LowSpeed9V, ColorAddress) { }

        /// <summary>
        /// Initialize this sensor
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
        }

        /// <summary>
		/// Returns the color index number (more on http://www.hitechnic.com/)
        /// </summary>
		public int ReadColorIndex()
        {
            return ReadRegister((byte)ColorRegister.ColorNumber, 1)[0]; ;
        }

        /// <summary>
        /// Reads the RGB colors.
        /// </summary>
        /// <returns>
        /// The RGB colors
        /// </returns>
        public RGBColor ReadRGBColor()
        {
            byte[] result = ReadRegister((byte)ColorRegister.RedReading, 3);
            return new RGBColor(result[0], result[1], result[2]);
        }

        /// <summary>
        /// Reads the normalized RGB colors
        /// </summary>
        /// <returns>
        /// The normalized RGB colors
        /// </returns>
        public RGBColor ReadNormalizedRGBColor()
        {
            byte[] result = ReadRegister((byte)ColorRegister.RedNormalized, 3);
            return new RGBColor(result[0], result[1], result[2]);
        }

        /// <summary>
        /// Reads the sensor value as a string.
        /// </summary>
        /// <returns>
        /// The value as a string
        /// </returns>
        public override string ReadAsString()
        {
            RGBColor color = ReadRGBColor();
            return "Red:" + color.Red + " green:" + color.Green + " blue:" + color.Blue;
        }
    }
    #endregion

    #region HiTechnic tilt sensor
    internal enum TiltRegister : byte
    {
        Version = 0x00, ProductId = 0x08, SensorType = 0x10, MeasurementUnits = 0x14,
        XHigh = 0x42, YHigh = 0x43, ZHigh = 0x44, XLow = 0x45, YLow = 0x46,
        ZLow = 0x47
    };

    /// <summary>
    /// X Y Z position
    /// </summary>
    public class Position
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Position"/> class.
        /// </summary>
        /// <param name='x'>
        /// The x coordinate.
        /// </param>
        /// <param name='y'>
        /// The y coordinate.
        /// </param>
        /// <param name='z'>
        /// The z coordinate.
        /// </param>
        public Position(int x, int y, int z) { X = x; Y = y; Z = z; }

        /// <summary>
        /// Gets the x coordinate
        /// </summary>
        /// <value>
        /// The x coordinate
        /// </value>
        public int X { get; }

        /// <summary>
        /// Gets the y coordinate
        /// </summary>
        /// <value>
        /// The y coordinate
        /// </value>
        public int Y { get; }

        /// <summary>
        /// Gets the z coordinate
        /// </summary>
        /// <value>
        /// The z coordinate
        /// </value>
        public int Z { get; }
    }
    /// <summary>
    /// HiTechnic tilt sensor
    /// </summary>
    public class HiTecTilt : I2CBase
    {
        private const byte TiltAddress = 0x02;
        /// <summary>
        /// Initializes a new instance of the <see cref="HiTecTilt"/> class.
        /// </summary>
		public HiTecTilt() : base(I2CMode.LowSpeed9V, TiltAddress) { }

        /// <summary>
        /// Initialize the sensor
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
        }

        /// <summary>
        /// Reads the X Y Z position
        /// </summary>
        /// <returns>
        /// The X Y Z position
        /// </returns>
        public Position ReadPosition()
        {
            byte[] data = ReadRegister((byte)TiltRegister.XHigh, 6);
            int x, y, z;
            x = data[0];
            y = data[1];
            z = data[2];

            if (x > 127)
                x -= 256;
            x = (x * 4) + data[3];

            if (y > 127)
                y -= 256;
            y = (y * 4) + data[4];

            if (z > 172)
                z -= 256;
            z = (z * 4) + data[5];
            return new Position(x, y, z);
        }

        /// <summary>
        /// Reads the sensor value as a string.
        /// </summary>
        /// <returns>
        /// The value as a string
        /// </returns>
        public override string ReadAsString()
        {
            Position pos = ReadPosition();
            return "x:" + pos.X + " y:" + pos.Y + " z:" + pos.Z;
        }

    }


    #endregion

    #region HiTechnic compass sensor
    internal enum CompassRegister : byte
    {
        Version = 0x00, ProductId = 0x08, SensorType = 0x10, Command = 0x41, Degree = 0x42, DegreeHalf = 0x43
    };

    /// <summary>
    /// HiTechnic tilt compass sensor
    /// </summary>
    public class HiTecCompass : I2CBase
    {
        private const byte CompassAddress = 0x02;

        /// <summary>
        /// Initializes a new instance of the <see cref="HiTecCompass"/> class.
        /// </summary>
        public HiTecCompass() : base(I2CMode.LowSpeed9V, CompassAddress) { }

        /// <summary>
        /// Initialize the sensor
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
        }

        /// <summary>
        /// Read the direction of the compass
        /// </summary>
        public int ReadDirection()
        {
            byte[] result = ReadRegister((byte)CompassRegister.Degree, 2);
            return (result[0] * 2) + result[1];
        }

        /// <summary>
        /// Reads the sensor value as a string.
        /// </summary>
        /// <returns>
        /// The value as a string
        /// </returns>
        public override string ReadAsString()
        {
            return "Degrees: " + ReadDirection();
        }

    }


    #endregion

    #region PCF8574 8-bit I/O chip
    /// <summary>
    /// PCF8574 I/O chip
    /// </summary>
    public class PCF8574 : I2CBase
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="PCF8574"/> class with I2C address  0×20
        /// </summary>
        public PCF8574() : base(I2CMode.LowSpeed9V, 0x20) { }



        /// <summary>
        /// Initializes a new instance of the <see cref="PCF8574"/> class.
        /// </summary>
        /// <param name='address'>
        /// I2c address
        /// </param>
        public PCF8574(byte address) : base(I2CMode.LowSpeed9V, address) { }

        /// <summary>
        /// Initialize the sensor
        /// </summary>
		public override void Initialize()
        {
            base.Initialize();
        }

        /// <summary>
        /// Read the pins from the sensor (0-255)
        /// </summary>
        public int Read()
        {
            byte[] command = { I2CAddress };
            return I2CWriteAndRead(command, 1)[0];
        }

        /// <summary>
        /// Write to sensor 
        /// </summary>
        /// <param name='set'>
        /// Pins to set (0-255)
        /// </param>
        public void Write(byte set)
        {
            byte[] command = { I2CAddress, set };
            I2CWrite(command, 0, true);

        }

        /// <summary>
        /// Reads the sensor value as a string.
        /// </summary>
        /// <returns>
        /// The value as a string
        /// </returns>
        public override string ReadAsString()
        {
            return "I/O value: " + Read();
        }
    }

    #endregion

    #region PCF8591 8-bit ADC chip

    /// <summary>
    /// ADC port for use with the PCF8591 I2C chip
    /// </summary>
    public enum ADCPort : byte
    {
        Port0 = 0x00, Port1 = 0x01, Port2 = 0x02, Port3 = 0x03
    }

    /// <summary>
    /// PCF8591 chip with four input and four output ports
    /// </summary>
    public class PCF8591 : I2CBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PCF8591"/> class with I2C address 0x20
        /// </summary>
        public PCF8591() : base(I2CMode.LowSpeed9V, 0x20) { }


        /// <summary>
        /// Initializes a new instance of the <see cref="PCF8591"/> class.
        /// </summary>
        /// <param name='address'>
        /// I2C Address
        /// </param>
        public PCF8591(byte address) : base(I2CMode.LowSpeed9V, address) { }

        /// <summary>
        /// Initialize the sensor
        /// </summary>
		public override void Initialize()
        {
            base.Initialize();
        }

        /// <summary>
        /// Always returns 0 use other read function
        /// </summary>
        public int Read()
        {
            return 0;
        }

        /// <summary>
        /// Read the value on the specified port
        /// </summary>
        /// <param name='port'>
        /// Port to read from
        /// </param>
        public byte Read(ADCPort port)
        {
            byte[] command = { I2CAddress, (byte)port };
            return I2CWriteAndRead(command, 1)[0];
        }

        /// <summary>
        /// Write to the chip
        /// </summary>
        /// <param name='port'>
        /// Port to write to
        /// </param>
        /// <param name='value'>
        /// Value to write
        /// </param>
        public void Write(ADCPort port, byte value)
        {
            byte[] command = { I2CAddress, (byte)((byte)port | 0x40), value };
            I2CWrite(command, 0, true);
        }

        /// <summary>
        /// Reads the all ports as a string
        /// </summary>
        /// <returns>
        /// The value of all ports as a string
        /// </returns>
        public override string ReadAsString()
        {
            return "P0:" + Read(ADCPort.Port0) + " P1:" + Read(ADCPort.Port1) + " P2:" + Read(ADCPort.Port2) + " P3:" + Read(ADCPort.Port3);
        }

    }
    #endregion

}
