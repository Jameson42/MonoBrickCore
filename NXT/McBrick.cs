namespace MonoBrick.NXT
{

    /// <summary>
    /// Class for mindstorms brick
    /// </summary>
    public class McNXTBrick<TSensor1, TSensor2, TSensor3, TSensor4> : Brick<TSensor1, TSensor2, TSensor3, TSensor4>
		where TSensor1 : Sensor, new()
        where TSensor2 : Sensor, new()
        where TSensor3 : Sensor, new()
        where TSensor4 : Sensor, new()
    {
        #region wrapper for motor
        private void Init()
        {
            MotorControlProxy = new MotorControlProxy(Mailbox);
            McMotorA.Connection = Connection;
            McMotorA.Port = MotorPort.OutA;
            McMotorA.MCProxy = MotorControlProxy;
            McMotorA.MCPort = MotorControlMotorPort.PortA;
            McMotorB.Connection = Connection;
            McMotorB.Port = MotorPort.OutB;
            McMotorB.MCProxy = MotorControlProxy;
            McMotorB.MCPort = MotorControlMotorPort.PortB;
            McMotorC.Connection = Connection;
            McMotorC.Port = MotorPort.OutC;
            McMotorC.MCProxy = MotorControlProxy;
            McMotorC.MCPort = MotorControlMotorPort.PortC;

            //Synchronized Motors
            McMotorAB.MCProxy = MotorControlProxy;
            McMotorAB.MCPort = MotorControlMotorPort.PortsAB;
            McMotorAC.MCProxy = MotorControlProxy;
            McMotorAC.MCPort = MotorControlMotorPort.PortsAC;
            McMotorBC.MCProxy = MotorControlProxy;
            McMotorBC.MCPort = MotorControlMotorPort.PortsBC;
            McMotorABC.MCProxy = MotorControlProxy;
            McMotorABC.MCPort = MotorControlMotorPort.PortsABC;
        }

        /// <summary>
        /// Initialize the MotorControl program on the brick.
        /// McMotors will not work if this method was not called beforehand
        /// </summary>
        public void InitMC()
        {
            if (!IsMotorControlOnNxt())
            {
                UploadMotorControlToNXT();
            }
            if (!IsMotorControlRunningOnNxt())
            {
                StartMotorControl();
            }
        }

        /// <summary>
        /// Motor A (MotorControl)
        /// </summary>
        /// <value>
        /// The motor connected to port A
        /// </value>
        public McMotor McMotorA { get; } = new McMotor();

        /// <summary>
        /// Motor B (MotorControl)
        /// </summary>
        /// <value>
        /// The motor connected to port B
        /// </value>
        public McMotor McMotorB { get; } = new McMotor();

        /// <summary>
        /// Motor C (MotorControl)
        /// </summary>
        /// <value>
        /// The motor connected to port C
        /// </value>
        public McMotor McMotorC { get; } = new McMotor();

        /// <summary>
        /// Motor AB (MotorControl)
        /// </summary>
        /// <value>
        /// The motor connected to ports AB
        /// </value>
        public McSyncMotor McMotorAB { get; } = new McSyncMotor();

        /// <summary>
        /// Motor AC (MotorControl)
        /// </summary>
        /// <value>
        /// The motor connected to ports AC
        /// </value>
        public McSyncMotor McMotorAC { get; } = new McSyncMotor();

        /// <summary>
        /// Motor BC (MotorControl)
        /// </summary>
        /// <value>
        /// The motor connected to ports BC
        /// </value>
        public McSyncMotor McMotorBC { get; } = new McSyncMotor();

        /// <summary>
        /// Motor ABC (MotorControl)
        /// </summary>
        /// <value>
        /// The motor connected to ports ABC
        /// </value>
        public McSyncMotor McMotorABC { get; } = new McSyncMotor();

        /// <summary>
        /// Gets the motor control proxy.
        /// </summary>
        /// <value>
        /// The motor control proxy.
        /// </value>
        public MotorControlProxy MotorControlProxy { get; private set; }

        /// <summary>
        /// Initializes a new instance of the Brick class.
        /// </summary>
        /// <param name='connection'>
        /// Connection to use
        /// </param>
        public McNXTBrick(Connection<Command, Reply> connection) : base(connection)
        {
            Init();
        }

        /// <summary>
        /// Initializes a new instance of the Brick class with bluetooth or usb connection
        /// </summary>
        /// <param name='connection'>
        /// Can either be a serial port name for bluetooth connection or "usb" for usb connection
        /// </param>
        public McNXTBrick(string connection) : base(connection)
        {
            Init();
        }

        /// <summary>
        /// Initializes a new instance of the Brick class with a network connection
        /// </summary>
        /// <param name='ipAddress'>
        /// The IP address to use
        /// </param>
        /// <param name='port'>
        /// The port number to use
        /// </param>
        public McNXTBrick(string ipAddress, ushort port) : base(ipAddress, port)
        {
            Init();
        }

        #endregion


        #region Helper utilities for MotorControl

        private static readonly string MotorControlFile = "MotorControl22.rxe";

        /// <summary>
        /// <para>Queries if the MotorControl-program is on the NXT</para>
        /// </summary>
        /// <returns>True if the MotorControl-program is on the NXT, false if not</returns>
        public bool IsMotorControlOnNxt()
        {
            IBrickFile[] files = FileSystem.FileList();
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].Name == MotorControlFile)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// <para>Uploads the MotorControl program onto the NXT</para>
        /// </summary>
        public void UploadMotorControlToNXT()
        {
            FileSystem.UploadFile(MotorControlFile, MotorControlFile);
        }

        /// <summary>
        /// <para>Queries if the MotorControl-program is currently running on the NXT.</para>
        /// </summary>
        /// <returns>True if the MotorControl-program is running, false if not</returns>
        public bool IsMotorControlRunningOnNxt()
        {
            try
            {
                return (GetRunningProgram() == MotorControlFile);
            }
            catch (MonoBrick.NXT.BrickException)
            {
                return false;
            }
        }

        /// <summary>
        /// <para>Starts the MotorControl-program.</para>
        /// </summary>
        public void StartMotorControl()
        {
            StartProgram(MotorControlFile, true);
        }

        /// <summary>
        /// <para>Stops the MotorControl-program.</para>
        /// </summary>
        /// <remarks>
        /// <para>Stops any running program, even it the program is not the MotorControl-program.</para>
        /// </remarks>
        public void StopMotorControl()
        {
            StopProgram();
        }

        #endregion
    }
}
