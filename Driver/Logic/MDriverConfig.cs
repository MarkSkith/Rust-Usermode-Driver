namespace MDriver.Logic
{
    using System.IO;

    using global::MDriver.Logic.Enums;

    public class MDriverConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MDriverConfig"/> class.
        /// </summary>
        public MDriverConfig()
        {
            this.SharedMemory = new MDriverConfigSharedMemory();
        }

        /// <summary>
        /// Gets the name of the service.
        /// </summary>
        public string ServiceName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the driver/system file.
        /// </summary>
        public FileInfo DriverFile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the symbolic link path.
        /// </summary>
        public string SymbolicLink
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the driver loading method.
        /// </summary>
        public MDriverLoad LoadMethod
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the IO communication method.
        /// </summary>
        public MIoMethod IoMethod
        {
            get;
            set;
        } = MIoMethod.IoControl;

        /// <summary>
        /// Gets the shared memory configuration.
        /// </summary>
        public MDriverConfigSharedMemory SharedMemory
        {
            get;
            set;
        }

        /// <summary>
        /// The shared memory configuration class.
        /// </summary>
        public class MDriverConfigSharedMemory
        {
            /// <summary>
            /// Gets or sets the process identifier.
            /// </summary>
            public int ProcessId
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the process address.
            /// </summary>
            public ulong ProcessAddr
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the first event name.
            /// </summary>
            public string FirstEventName
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the second event name.
            /// </summary>
            public string SecondEventName
            {
                get;
                set;
            }
        }
    }
}
