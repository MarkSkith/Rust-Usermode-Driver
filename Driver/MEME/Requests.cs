using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDriver.MEME
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using MDriver.MEME.Structures;
    using MDriver.Logic;
    using MDriver.Native;

    using Microsoft.Win32.SafeHandles;

    public partial class Requests
    {

        /// <summary>
        /// Gets the driver.
        /// </summary>
        internal MDriver Driver
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the lastly used process identifier.
        /// </summary>
        public ulong LastProcessId
        {
            get;
            private set;
        }
        public const uint WriteCtlCode_Protection = (0x22 << 16) | (0x00 << 14) | (0x0823 << 2) | 0x00;
        public const uint WriteCtlCode = (0x22 << 16) | (0x00 << 14) | (0x0824 << 2) | 0x00;
        public const uint ReadCtlCode = (0x22 << 16) | (0x00 << 14) | (0x0825 << 2) | 0x00;
        public const uint GetBaseCtlCode = (0x22 << 16) | (0x00 << 14) | (0x0826 << 2) | 0x00;
        public const uint GetUPBaseCtlCode = (0x22 << 16) | (0x00 << 14) | (0x0827 << 2) | 0x00;
        public const uint GetGABaseCtlCode = (0x22 << 16) | (0x00 << 14) | (0x0828 << 2) | 0x00;
        public const uint CtlCode_Scan = (0x22 << 16) | (0x00 << 14) | (0x0829 << 2) | 0x00;

        /// <summary>
        /// Initializes a new instance of the <see cref="Requests"/> class.
        /// </summary>
        public Requests(string Game)
        {
            LoadMemory(Game);
            // Requests.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Requests"/> class.
        /// </summary>
        /// <param name="Driver">The driver.</param>
        /// <exception cref="ArgumentException">Driver is null.</exception>
        //internal Requests(MDriver Driver) : this()
        //{
        //    this.SetDriver(Driver);
        //}

        /// <summary>
        /// Sets the driver.
        /// </summary>
        /// <param name="Driver">The driver.</param>
        /// <exception cref="System.ArgumentNullException">Driver - Driver is null</exception>
        private void SetDriver(MDriver Driver)
        {
            if (Driver == null)
            {
                throw new ArgumentNullException(nameof(Driver), "Driver is null");
            }

            this.Driver = Driver;
        }

        /// <summary>
        /// Sets the process identifier.
        /// </summary>
        /// <param name="ProcId">The process identifier.</param>
        private void SetProcId(ulong ProcId)
        {
            this.LastProcessId = ProcId;
        }

        public static bool IsValidPtr(ulong Address)
        {
            return Address != 0 && Address > 0x100000 && Address < 0x7FFFFFFFFFFFFF;
        }

        #region Reading CTRL

        private byte[] Read(ulong Address, Int32 Size)
        {
            if (!IsValidPtr(Address))
            {
                var NullBuffer = new byte[Size];
                return NullBuffer;
            }

            var Success = false;
            var Request = new KernelReadRequest();
            var RequestSize = (uint)Marshal.SizeOf<KernelReadRequest>();
            var Buffer = new byte[Size];
            var Allocation = GCHandle.Alloc(Buffer, GCHandleType.Pinned);
            var BufferPtr = Allocation.AddrOfPinnedObject();

            if (!Allocation.IsAllocated)
            {
                throw new InsufficientMemoryException("Couldn't allocate memory for the buffer, at Read(ProcessId, Address, Size).");
            }

            Request.ProcessId = this.LastProcessId;
            Request.Size = Size;
            Request.Response = (ulong)BufferPtr.ToInt64();
            Request.Address = Address;

            Success = this.Driver.IO.TryIoControl(ReadCtlCode, Request, (int)RequestSize);

            if (Success)
            {
                //Debug.WriteLine(Request.Response.ToString("x"));
                Buffer = (byte[])Allocation.Target;
            }

            Allocation.Free();

            if (!Success)
            {
                //Debug.WriteLine("IOCTL Failed!");
                Buffer = null;
            }

            return Buffer;
        }
        #endregion

        #region Writing CTRL
        private void Write(ulong Address, byte[] Value, bool ProtectionBypass = false) //ProtectionBypass can cause BSOD if called in succection to quickly
        {
            if (!IsValidPtr(Address))
            {
                return;
            }
            var Size = Value.Length;

            var Success = false;
            var Request = new KernelWriteRequest();
            var RequestSize = (uint)Marshal.SizeOf<KernelWriteRequest>();
            var Allocation = GCHandle.Alloc(Value, GCHandleType.Pinned);

            if (!Allocation.IsAllocated)
            {
                throw new InsufficientMemoryException("Couldn't allocate memory for the buffer, at Write<T>(Address, Value, UseBaseAddress).");
            }

            Request.ProcessId = this.LastProcessId;
            Request.Size = Size;
            Request.Value = (ulong)Allocation.AddrOfPinnedObject().ToInt64();
            Request.Address = Address;

            if (ProtectionBypass)
            {
                Success = this.Driver.IO.TryIoControl(WriteCtlCode_Protection, Request, (int)RequestSize);
            }
            else
            {
                Success = this.Driver.IO.TryIoControl(WriteCtlCode, Request, (int)RequestSize);
            }


            Allocation.Free();

            if (!Success)
            {
                throw new Exception("Failed to write the given structure to the specified Address, at Write<T>(Address, Value, UseBaseAddress).");
            }
        }

        #endregion        

        public ulong GetGameBase()
        {
            if (!this.Driver.IO.IsConnected)
            {
                throw new Exception("Driver is disconnected.");
            }

            var Success = false;
            var Request = new KernelReadRequest();
            var RequestSize = (uint)Marshal.SizeOf<KernelReadRequest>();
            var Buffer = new byte[8];
            var Allocation = GCHandle.Alloc(Buffer, GCHandleType.Pinned);
            var BufferPtr = Allocation.AddrOfPinnedObject();

            Request.ProcessId = this.LastProcessId;
            Request.Response = (ulong)BufferPtr.ToInt64();
            Request.Size = 8;

            Success = this.Driver.IO.TryIoControl<KernelReadRequest>((uint)GetBaseCtlCode, (KernelReadRequest)Request, (int)RequestSize);
            if (Success)
            {
                //Debug.WriteLine("IOCTL Success!");
                Buffer = (byte[])Allocation.Target;
                return (ulong)BitConverter.ToInt64(Buffer, 0);
            }
            else
            {
                //Debug.WriteLine("IOCTL Failed!");
                return 0;
            }
        }

        public enum ModuleName
        {
            UnityPlayer,
            GameAssembly
        }

        public ulong GetModuleBase(ModuleName modulename)
        {
            if (!this.Driver.IO.IsConnected)
            {
                throw new Exception("Driver is disconnected.");
            }

            var Success = false;
            var Request = new KernelReadRequest();
            var RequestSize = (uint)Marshal.SizeOf<KernelReadRequest>();
            var Buffer = new byte[8];
            var Allocation = GCHandle.Alloc(Buffer, GCHandleType.Pinned);
            var BufferPtr = Allocation.AddrOfPinnedObject();

            Request.ProcessId = this.LastProcessId;
            Request.Response = (ulong)BufferPtr.ToInt64();
            Request.Size = 8;
            //Request.data = DLL;
            //Request.dataSize = (ulong)Request.data.Length;
            //Request.dataAddress = (ulong)(BufferPtr + 0xF4).ToInt64();
            //Debug.WriteLine("Datta ADDR: " + Request.dataAddress.ToString("X"));

            if (modulename == ModuleName.UnityPlayer)
            {
                Success = this.Driver.IO.TryIoControl<KernelReadRequest>((uint)GetUPBaseCtlCode, (KernelReadRequest)Request, (int)RequestSize);
            }
            else if (modulename == ModuleName.GameAssembly)
            {
                Success = this.Driver.IO.TryIoControl<KernelReadRequest>((uint)GetGABaseCtlCode, (KernelReadRequest)Request, (int)RequestSize);
            }

            if (Success)
            {
                //Debug.WriteLine("IOCTL Success!");
                Buffer = (byte[])Allocation.Target;
                return (ulong)BitConverter.ToInt64(Buffer, 0);
            }
            else
            {
                //Debug.WriteLine("IOCTL Failed!");
                return 0;
            }
        }

        public void Unload()
        {
            this.Driver.IO.Driver.Unload();
            //if (this.Driver.IO.TryIoControl(UnloadCtlCode))
            //{
            //    Debug.WriteLine("Driver Unloaded!");
            //}
            //else
            //{
            //    Debug.WriteLine("Failed To Unload Driver!");
            //}
        }

        #region Memory class stuff
        public ulong GameBase = 0x0;
        public bool IsAttached = false;

        private void LoadMemory(string Game)
        {
            Debug.WriteLine("[*] Loading...");
            var SystemFile = new FileInfo("C:\\Windows\\System32\\drivers\\wmiacpi.sys");
            if (SystemFile.Exists)
            {
                Debug.WriteLine("[*] SystemFile Found.");
                var Driver = new MDriver(new MDriverConfig()
                {
                    ServiceName = "RussiaBest", //This is the same as SymbolicLink basically
                    SymbolicLink = @"\\.\RussiaBest", //This is what the drivers SymbolicLink name is to connect and write to the cheat
                    DriverFile = SystemFile,
                    LoadMethod = global::MDriver.Logic.Enums.MDriverLoad.Normal
                });

                if (global::MDriver.Logic.MDriver.CanConnectTo(Driver.Config.SymbolicLink, Driver.Config.IoMethod))
                {
                    Debug.WriteLine("[*] The driver symbolic file is already created.");
                    Debug.WriteLine("[*] Please make sure you are not loading the driver twice!");
                }
                try
                {
                    if (Driver.Load())
                    {
                        Debug.WriteLine("[*] Driver has been loaded.");
                        Debug.WriteLine((string)("[*] Driver->Handle       : 0x" + Driver.IO.Handle?.DangerousGetHandle().ToString("X").PadLeft(8, '0')));
                        Debug.WriteLine((string)("[*] Driver->IsLoaded     : " + Driver.IsLoaded));
                        Debug.WriteLine((string)("[*] Driver->IsConnected  : " + Driver.IO.IsConnected));
                        Debug.WriteLine((string)("[*] Driver->IsDisposed   : " + Driver.IsDisposed));

                        if (Driver.IO.IsConnected)
                        {
                            this.SetDriver(Driver);
                            ViewMatrix.MEMAPI = this;
                            //this.Driver.IO.IsConnected = true;
                            SetProcId((ulong)Process.GetProcessesByName(Game)[0].Id);
                            Debug.WriteLine("ProcID : " + (string)LastProcessId.ToString());

                            GameBase = GetGameBase();
                            Debug.WriteLine("Game Address : 0x" + GameBase.ToString("X"));

                            if (GameBase > 100 && LastProcessId > 0)
                            {
                                IsAttached = true;
                                Debug.WriteLine((string)("[*] Game->IsAttached   : " + IsAttached));
                            }

                            //Console.WriteLine(GetUnityPlayerDllBase().ToString("X"));
                        }
                        else
                        {
                            Debug.WriteLine("[*] Failed to initialize the IO communication.");
                        }

                    }
                    else
                    {
                        Debug.WriteLine("[*] Failed to load the driver.");
                    }
                }
                catch (Exception Exception)
                {
                    Debug.WriteLine("[*] " + Exception.Message + ".");
                }
            }
            else
            {
                Debug.WriteLine("SystemFile Not Found!.");
            }
        }

        public byte[] ReadBytes(ulong Address, Int32 Length = 4)
        {
            if (IsValidPtr(Address) && IsAttached)
            {
                byte[] Read_Bytes = new byte[Length];
                Read_Bytes = this.Read(Address, Length);
                return Read_Bytes;
            }
            else
            {
                byte[] Read_Bytes = new byte[Length];
                return Read_Bytes;
            }
        }

        public byte Readbyte(ulong Address)
        {
            if (IsValidPtr(Address) && IsAttached)
            {
                byte[] buffer = new byte[1];
                buffer = this.ReadBytes(Address, 1);

                if (buffer != null)
                {
                    return buffer[0];
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        public Int16 ReadInt16(ulong Address)
        {
            if (IsValidPtr(Address) && IsAttached)
            {
                byte[] buffer = new byte[2];
                buffer = ReadBytes(Address, 2);

                if (buffer != null)
                {
                    return BitConverter.ToInt16(buffer, 0);
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        public Int32 ReadInt32(ulong Address)
        {
            if (IsValidPtr(Address) && IsAttached)
            {
                byte[] buffer = new byte[4];
                buffer = ReadBytes(Address, 4);

                if (buffer != null)
                {
                    return BitConverter.ToInt32(buffer, 0);
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        public ulong ReadInt64(ulong Address)
        {
            if (IsValidPtr(Address) && IsAttached)
            {
                byte[] buffer = new byte[8];
                buffer = ReadBytes(Address, 8);

                if (buffer != null)
                {
                    return (ulong)BitConverter.ToInt64(buffer, 0);
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        public float ReadFloat(ulong Address)
        {
            if (IsValidPtr(Address) && IsAttached)
            {
                byte[] buffer = new byte[4];
                buffer = ReadBytes(Address, 4);

                if (buffer != null)
                {
                    return BitConverter.ToSingle(buffer, 0);
                }
                else
                {
                    return 0f;
                }
            }
            else
            {
                return 0f;
            }
        }

        public double ReadDouble(ulong Address)
        {
            if (IsValidPtr(Address) && IsAttached)
            {
                byte[] buffer = new byte[8];
                buffer = ReadBytes(Address, 8);

                if (buffer != null)
                {
                    return BitConverter.ToDouble(buffer, 0);
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        public ulong GetPointer(params ulong[] args)
        {
            if (IsValidPtr(args[0]) && IsAttached)
            {
                ulong curaddr = 0;
                if (args.Length <= 1)
                {
                    return 0;
                }
                else
                {
                    for (int i = 0; i <= args.Count() - 1; i++)
                    {
                        if (i == 0)
                        {
                            curaddr = ReadInt64(args[i]);
                        }
                        else if (i != args.Count() - 1)
                        {
                            if (curaddr == 0)
                            {
                                return 0;
                            }

                            curaddr = ReadInt64(curaddr + args[i]);
                        }
                        else
                        {
                            curaddr += args[i];
                        }
                    }
                    if (IsValidPtr(curaddr))
                    {
                        return curaddr;
                    }
                    else
                    {
                        return 0;
                    }

                }
            }
            else
            {
                return 0;
            }
        }

        public string ReadString(ulong Address, Int32 Length = 124, bool Unicode = false)
        {
            if (Unicode)
            {
                Length = Length * 2;
            }
            if (IsValidPtr(Address) && IsAttached)
            {
                if (Length > 0)
                {
                    byte[] buffer = new byte[Length];
                    buffer = ReadBytes(Address, Length);
                    if (buffer != null)
                    {
                        if (!Unicode)
                        {
                            var enc = new ASCIIEncoding();
                            for (int i = 0; i <= buffer.Length - 1; i++)
                            {
                                if (buffer[i] == 0x0)
                                {
                                    Array.Resize(ref buffer, i);
                                    return enc.GetString(buffer);
                                }
                            }
                            return enc.GetString(buffer);
                        }
                        else
                        {
                            var enc = new UnicodeEncoding();
                            return enc.GetString(buffer);
                        }
                    }
                    else
                    {
                        return "";
                    }

                }
                else
                {
                    return "";
                }
            }
            else
            {
                return "";
            }
        }

        #region Vector Operations

        #region Read and Write
        public Vector2.Vector2f ReadVector2f(ulong Address)
        {
            if (IsValidPtr(Address) && IsAttached)
            {
                byte[] buffer = ReadBytes(Address, 0x4 * 2);
                var floatArray = new float[buffer.Length / 4];
                Buffer.BlockCopy(buffer, 0, floatArray, 0, buffer.Length);

                return new Vector2.Vector2f(floatArray[0], floatArray[1]);
            }
            else
            {
                return new Vector2.Vector2f(0);
            }
        }

        public void WriteVector2f(ulong Address, Vector2.Vector2f value)
        {
            if (IsValidPtr(Address) && IsAttached)
            {
                var floatArray = new float[] { value.x, value.y };
                var byteArray = new byte[floatArray.Length * 4];
                Buffer.BlockCopy(floatArray, 0, byteArray, 0, byteArray.Length);

                WriteBytes(Address, byteArray);
            }
        }

        public Vector3.Vector3f ReadVector3f(ulong Address)
        {
            if (IsValidPtr(Address) && IsAttached)
            {
                byte[] buffer = ReadBytes(Address, 0x4 * 3);
                var floatArray = new float[buffer.Length / 4];
                Buffer.BlockCopy(buffer, 0, floatArray, 0, buffer.Length);

                return new Vector3.Vector3f(floatArray[0], floatArray[1], floatArray[2]);
            }
            else
            {
                return new Vector3.Vector3f(0);
            }
        }

        public void WriteVector3f(ulong Address, Vector3.Vector3f value)
        {
            if (IsValidPtr(Address) && IsAttached)
            {
                var floatArray = new float[] { value.x, value.y, value.z };
                var byteArray = new byte[floatArray.Length * 4];
                Buffer.BlockCopy(floatArray, 0, byteArray, 0, byteArray.Length);

                WriteBytes(Address, byteArray);
            }
        }

        public Vector4.Vector4f ReadVector4f(ulong Address)
        {
            if (IsValidPtr(Address) && IsAttached)
            {
                byte[] buffer = ReadBytes(Address, 0x4 * 4);
                var floatArray = new float[buffer.Length / 4];
                Buffer.BlockCopy(buffer, 0, floatArray, 0, buffer.Length);

                return new Vector4.Vector4f(floatArray[0], floatArray[1], floatArray[2], floatArray[3]);
            }
            else
            {
                return new Vector4.Vector4f(0);
            }
        }

        public void WriteVector4f(ulong Address, Vector4.Vector4f value)
        {
            if (IsValidPtr(Address) && IsAttached)
            {
                var floatArray = new float[] { value.x, value.y, value.z, value.w };
                var byteArray = new byte[floatArray.Length * 4];
                Buffer.BlockCopy(floatArray, 0, byteArray, 0, byteArray.Length);

                WriteBytes(Address, byteArray);
            }
        }
        #endregion

        public partial class Vector2
        {
            public struct Vector2f
            {
                internal float x;
                internal float y;

                public float X { get { return x; } set { x = value; } }
                public float Y { get { return y; } set { y = value; } }


                public static Vector2f One
                {
                    get { return new Vector2f(1); }
                }

                public static Vector2f Zero
                {
                    get { return new Vector2f(0); }
                }

                public static Vector2f MinusOne
                {
                    get { return new Vector2f(-1); }
                }

                [System.Runtime.CompilerServices.IndexerName("Component")]
                public unsafe float this[int index]
                {
                    get
                    {
                        if ((index | 0x3) != 0x3) //index < 0 || index > 3
                            throw new ArgumentOutOfRangeException("index");
                        fixed (float* v = &x)
                        {
                            return *(v + index);
                        }
                    }
                    set
                    {
                        if ((index | 0x3) != 0x3) //index < 0 || index > 3
                            throw new ArgumentOutOfRangeException("index");
                        fixed (float* v = &x)
                        {
                            *(v + index) = value;
                        }
                    }
                }

                public Vector2f(float x, float y)
                {
                    this.x = x;
                    this.y = y;
                }

                public Vector2f(float f)
                {
                    this.x = f;
                    this.y = f;
                }

                public static float Dot(Vector2f left, Vector2f right)
                {
                    return (left.x * right.x) + (left.y * right.y);
                }

                public static float Distance(Vector2f value1, Vector2f value2)
                {
                    float x = value1.x - value2.x;
                    float y = value1.y - value2.y;

                    return (float)Math.Sqrt((x * x) + (y * y));
                }

                public static unsafe Vector2f operator &(Vector2f v1, Vector2f v2)
                {
                    Vector2f res = new Vector2f();
                    int* a = (int*)&v1;
                    int* b = (int*)&v2;
                    int* c = (int*)&res;
                    *c++ = *a++ & *b++;
                    *c++ = *a++ & *b++;
                    *c++ = *a++ & *b++;
                    *c = *a & *b;
                    return res;
                }

                public static unsafe Vector2f operator |(Vector2f v1, Vector2f v2)
                {
                    Vector2f res = new Vector2f();
                    int* a = (int*)&v1;
                    int* b = (int*)&v2;
                    int* c = (int*)&res;
                    *c++ = *a++ | *b++;
                    *c++ = *a++ | *b++;
                    *c++ = *a++ | *b++;
                    *c = *a | *b;
                    return res;
                }

                public static unsafe Vector2f operator ^(Vector2f v1, Vector2f v2)
                {
                    Vector2f res = new Vector2f();
                    int* a = (int*)&v1;
                    int* b = (int*)&v2;
                    int* c = (int*)&res;
                    *c++ = *a++ ^ *b++;
                    *c++ = *a++ ^ *b++;
                    *c++ = *a++ ^ *b++;
                    *c = *a ^ *b;
                    return res;
                }

                public static Vector2f operator +(Vector2f v1, Vector2f v2)
                {
                    return new Vector2f(v1.x + v2.x, v1.y + v2.y);
                }

                public static Vector2f operator -(Vector2f v1, Vector2f v2)
                {
                    return new Vector2f(v1.x - v2.x, v1.y - v2.y);
                }

                public static Vector2f operator *(Vector2f v1, Vector2f v2)
                {
                    return new Vector2f(v1.x * v2.x, v1.y * v2.y);
                }

                public static Vector2f operator *(float scalar, Vector2f v)
                {
                    return new Vector2f(scalar * v.x, scalar * v.y);
                }

                public static Vector2f operator *(Vector2f v, float scalar)
                {
                    return new Vector2f(scalar * v.x, scalar * v.y);
                }

                public static Vector2f operator /(Vector2f v1, Vector2f v2)
                {
                    return new Vector2f(v1.x / v2.x, v1.y / v2.y);
                }

                public static bool operator ==(Vector2f v1, Vector2f v2)
                {
                    return v1.x == v2.x && v1.y == v2.y;
                }

                public static bool operator !=(Vector2f v1, Vector2f v2)
                {
                    return v1.x != v2.x || v1.y != v2.y;
                }

                public override string ToString()
                {
                    return "<" + x + ", " + y + ">";
                }
            }
        }

        public partial class Vector3
        {
            public struct Vector3f
            {
                internal float x;
                internal float y;
                internal float z;

                public float X { get { return x; } set { x = value; } }
                public float Y { get { return y; } set { y = value; } }
                public float Z { get { return z; } set { z = value; } }

                public static Vector3f One
                {
                    get { return new Vector3f(1); }
                }

                public static Vector3f Zero
                {
                    get { return new Vector3f(0); }
                }

                public static Vector3f MinusOne
                {
                    get { return new Vector3f(-1); }
                }

                [System.Runtime.CompilerServices.IndexerName("Component")]
                public unsafe float this[int index]
                {
                    get
                    {
                        if ((index | 0x3) != 0x3) //index < 0 || index > 3
                            throw new ArgumentOutOfRangeException("index");
                        fixed (float* v = &x)
                        {
                            return *(v + index);
                        }
                    }
                    set
                    {
                        if ((index | 0x3) != 0x3) //index < 0 || index > 3
                            throw new ArgumentOutOfRangeException("index");
                        fixed (float* v = &x)
                        {
                            *(v + index) = value;
                        }
                    }
                }

                public Vector3f(float x, float y, float z)
                {
                    this.x = x;
                    this.y = y;
                    this.z = z;
                }

                public Vector3f(float f)
                {
                    this.x = f;
                    this.y = f;
                    this.z = f;
                }

                public static float Dot(Vector3f left, Vector3f right)
                {
                    return (left.x * right.x) + (left.y * right.y) + (left.z * right.z);
                }

                public static float Distance(Vector3f value1, Vector3f value2)
                {
                    float x = value1.x - value2.x;
                    float y = value1.y - value2.y;
                    float z = value1.z - value2.z;

                    return (float)Math.Sqrt((x * x) + (y * y) + (z * z));
                }

                public static unsafe Vector3f operator &(Vector3f v1, Vector3f v2)
                {
                    Vector3f res = new Vector3f();
                    int* a = (int*)&v1;
                    int* b = (int*)&v2;
                    int* c = (int*)&res;
                    *c++ = *a++ & *b++;
                    *c++ = *a++ & *b++;
                    *c++ = *a++ & *b++;
                    *c = *a & *b;
                    return res;
                }

                public static unsafe Vector3f operator |(Vector3f v1, Vector3f v2)
                {
                    Vector3f res = new Vector3f();
                    int* a = (int*)&v1;
                    int* b = (int*)&v2;
                    int* c = (int*)&res;
                    *c++ = *a++ | *b++;
                    *c++ = *a++ | *b++;
                    *c++ = *a++ | *b++;
                    *c = *a | *b;
                    return res;
                }

                public static unsafe Vector3f operator ^(Vector3f v1, Vector3f v2)
                {
                    Vector3f res = new Vector3f();
                    int* a = (int*)&v1;
                    int* b = (int*)&v2;
                    int* c = (int*)&res;
                    *c++ = *a++ ^ *b++;
                    *c++ = *a++ ^ *b++;
                    *c++ = *a++ ^ *b++;
                    *c = *a ^ *b;
                    return res;
                }

                public static Vector3f operator +(Vector3f v1, Vector3f v2)
                {
                    return new Vector3f(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
                }

                public static Vector3f operator -(Vector3f v1, Vector3f v2)
                {
                    return new Vector3f(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
                }

                public static Vector3f operator *(Vector3f v1, Vector3f v2)
                {
                    return new Vector3f(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
                }

                public static Vector3f operator *(float scalar, Vector3f v)
                {
                    return new Vector3f(scalar * v.x, scalar * v.y, scalar * v.z);
                }

                public static Vector3f operator *(Vector3f v, float scalar)
                {
                    return new Vector3f(scalar * v.x, scalar * v.y, scalar * v.z);
                }

                public static Vector3f operator /(Vector3f v1, Vector3f v2)
                {
                    return new Vector3f(v1.x / v2.x, v1.y / v2.y, v1.z / v2.z);
                }

                public static bool operator ==(Vector3f v1, Vector3f v2)
                {
                    return v1.x == v2.x && v1.y == v2.y && v1.z == v2.z;
                }

                public static bool operator !=(Vector3f v1, Vector3f v2)
                {
                    return v1.x != v2.x || v1.y != v2.y || v1.z != v2.z;
                }

                public override string ToString()
                {
                    return "<" + x + ", " + y + ", " + z + ">";
                }
            }
        }


        public partial class Vector4
        {
            public struct Vector4f
            {
                internal float x;
                internal float y;
                internal float z;
                internal float w;

                public float X { get { return x; } set { x = value; } }
                public float Y { get { return y; } set { y = value; } }
                public float Z { get { return z; } set { z = value; } }
                public float W { get { return w; } set { w = value; } }

                public static Vector4f Pi
                {
                    get { return new Vector4f((float)System.Math.PI); }
                }

                public static Vector4f E
                {
                    get { return new Vector4f((float)System.Math.E); }
                }

                public static Vector4f One
                {
                    get { return new Vector4f(1); }
                }

                public static Vector4f Zero
                {
                    get { return new Vector4f(0); }
                }

                public static Vector4f MinusOne
                {
                    get { return new Vector4f(-1); }
                }

                [System.Runtime.CompilerServices.IndexerName("Component")]
                public unsafe float this[int index]
                {
                    get
                    {
                        if ((index | 0x3) != 0x3) //index < 0 || index > 3
                            throw new ArgumentOutOfRangeException("index");
                        fixed (float* v = &x)
                        {
                            return *(v + index);
                        }
                    }
                    set
                    {
                        if ((index | 0x3) != 0x3) //index < 0 || index > 3
                            throw new ArgumentOutOfRangeException("index");
                        fixed (float* v = &x)
                        {
                            *(v + index) = value;
                        }
                    }
                }

                public Vector4f(float x, float y, float z, float w)
                {
                    this.x = x;
                    this.y = y;
                    this.z = z;
                    this.w = w;
                }

                public Vector4f(float f)
                {
                    this.x = f;
                    this.y = f;
                    this.z = f;
                    this.w = f;
                }

                public static float Dot(Vector4f left, Vector4f right)
                {
                    return (left.x * right.x) + (left.y * right.y) + (left.z * right.z) + (left.w * right.w);
                }

                public static float Distance(Vector4f value1, Vector4f value2)
                {
                    float x = value1.x - value2.x;
                    float y = value1.y - value2.y;
                    float z = value1.z - value2.z;
                    float w = value1.w - value2.w;

                    return (float)Math.Sqrt((x * x) + (y * y) + (z * z) + (w * w));
                }

                public static unsafe Vector4f operator &(Vector4f v1, Vector4f v2)
                {
                    Vector4f res = new Vector4f();
                    int* a = (int*)&v1;
                    int* b = (int*)&v2;
                    int* c = (int*)&res;
                    *c++ = *a++ & *b++;
                    *c++ = *a++ & *b++;
                    *c++ = *a++ & *b++;
                    *c = *a & *b;
                    return res;
                }

                public static unsafe Vector4f operator |(Vector4f v1, Vector4f v2)
                {
                    Vector4f res = new Vector4f();
                    int* a = (int*)&v1;
                    int* b = (int*)&v2;
                    int* c = (int*)&res;
                    *c++ = *a++ | *b++;
                    *c++ = *a++ | *b++;
                    *c++ = *a++ | *b++;
                    *c = *a | *b;
                    return res;
                }

                public static unsafe Vector4f operator ^(Vector4f v1, Vector4f v2)
                {
                    Vector4f res = new Vector4f();
                    int* a = (int*)&v1;
                    int* b = (int*)&v2;
                    int* c = (int*)&res;
                    *c++ = *a++ ^ *b++;
                    *c++ = *a++ ^ *b++;
                    *c++ = *a++ ^ *b++;
                    *c = *a ^ *b;
                    return res;
                }

                public static Vector4f operator +(Vector4f v1, Vector4f v2)
                {
                    return new Vector4f(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z, v1.w + v2.w);
                }

                public static Vector4f operator -(Vector4f v1, Vector4f v2)
                {
                    return new Vector4f(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z, v1.w - v2.w);
                }

                public static Vector4f operator *(Vector4f v1, Vector4f v2)
                {
                    return new Vector4f(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z, v1.w * v2.w);
                }

                public static Vector4f operator *(float scalar, Vector4f v)
                {
                    return new Vector4f(scalar * v.x, scalar * v.y, scalar * v.z, scalar * v.w);
                }

                public static Vector4f operator *(Vector4f v, float scalar)
                {
                    return new Vector4f(scalar * v.x, scalar * v.y, scalar * v.z, scalar * v.w);
                }

                public static Vector4f operator /(Vector4f v1, Vector4f v2)
                {
                    return new Vector4f(v1.x / v2.x, v1.y / v2.y, v1.z / v2.z, v1.w / v2.w);
                }

                public static bool operator ==(Vector4f v1, Vector4f v2)
                {
                    return v1.x == v2.x && v1.y == v2.y && v1.z == v2.z && v1.w == v2.w;
                }

                public static bool operator !=(Vector4f v1, Vector4f v2)
                {
                    return v1.x != v2.x || v1.y != v2.y || v1.z != v2.z || v1.w != v2.w;
                }

                public override string ToString()
                {
                    return "<" + x + ", " + y + ", " + z + ", " + w + ">";
                }
            }
        }

        #endregion

        public partial class ViewMatrix
        {
            internal static Requests MEMAPI;

            public float M11;
            public float M12;
            public float M13;
            public float M14;
            public float M21;
            public float M22;
            public float M23;
            public float M24;
            public float M31;
            public float M32;
            public float M33;
            public float M34;
            public float M41;
            public float M42;
            public float M43;
            public float M44;

            public ViewMatrix(float M11, float M12, float M13, float M14, float M21, float M22, float M23, float M24, float M31, float M32, float M33, float M34, float M41, float M42, float M43, float M44)
            {
                this.M11 = M11;
                this.M12 = M12;
                this.M13 = M13;
                this.M14 = M14;
                this.M21 = M21;
                this.M22 = M22;
                this.M23 = M23;
                this.M24 = M24;
                this.M31 = M31;
                this.M32 = M32;
                this.M33 = M33;
                this.M34 = M34;
                this.M41 = M41;
                this.M42 = M42;
                this.M43 = M43;
                this.M44 = M44;
            }

            public ViewMatrix(float f)
            {
                this.M11 = f;
                this.M12 = f;
                this.M13 = f;
                this.M14 = f;
                this.M21 = f;
                this.M22 = f;
                this.M23 = f;
                this.M24 = f;
                this.M31 = f;
                this.M32 = f;
                this.M33 = f;
                this.M34 = f;
                this.M41 = f;
                this.M42 = f;
                this.M43 = f;
                this.M44 = f;
            }

            public static ViewMatrix ReadViewMatrix(ulong Address)
            {
                if (IsValidPtr(Address) && MEMAPI.IsAttached)
                {
                    byte[] buffer = MEMAPI.ReadBytes(Address, 0x4 * 16);
                    var floatArray = new float[buffer.Length / 4];
                    Buffer.BlockCopy(buffer, 0, floatArray, 0, buffer.Length);

                    return new ViewMatrix(floatArray[0], floatArray[1], floatArray[2], floatArray[3], floatArray[4], floatArray[5], floatArray[6], floatArray[7], floatArray[8], floatArray[9], floatArray[10], floatArray[11], floatArray[12], floatArray[13], floatArray[14], floatArray[15]);
                }
                else
                {
                    return new ViewMatrix(0);
                }
            }

            public static void Transpose(ref ViewMatrix value, out ViewMatrix result)
            {
                ViewMatrix temp = new ViewMatrix(0);
                temp.M11 = value.M11;
                temp.M12 = value.M21;
                temp.M13 = value.M31;
                temp.M14 = value.M41;
                temp.M21 = value.M12;
                temp.M22 = value.M22;
                temp.M23 = value.M32;
                temp.M24 = value.M42;
                temp.M31 = value.M13;
                temp.M32 = value.M23;
                temp.M33 = value.M33;
                temp.M34 = value.M43;
                temp.M41 = value.M14;
                temp.M42 = value.M24;
                temp.M43 = value.M34;
                temp.M44 = value.M44;

                result = temp;
            }
        }

        #region Writing

        public void WriteBytes(ulong Address, byte[] buffer, bool ProtectionBypass = false)
        {
            Write(Address, buffer, ProtectionBypass);
        }

        public void WriteByte(ulong Address, byte Value, bool ProtectionBypass = false)
        {
            if (IsValidPtr(Address) && IsAttached)
            {
                byte[] buffer = new byte[1];
                buffer = BitConverter.GetBytes(Value);
                WriteBytes(Address, buffer, ProtectionBypass);
            }
        }

        public void WriteInt16(ulong Address, Int16 Value, bool ProtectionBypass = false)
        {
            if (IsValidPtr(Address) && IsAttached)
            {
                byte[] buffer = new byte[2];
                buffer = BitConverter.GetBytes(Value);
                WriteBytes(Address, buffer, ProtectionBypass);
            }
        }

        public void WriteInt32(ulong Address, Int32 Value, bool ProtectionBypass = false)
        {
            if (IsValidPtr(Address) && IsAttached)
            {
                byte[] buffer = new byte[4];
                buffer = BitConverter.GetBytes(Value);
                WriteBytes(Address, buffer, ProtectionBypass);
            }
        }

        public void WriteInt64(ulong Address, Int64 Value, bool ProtectionBypass = false)
        {
            if (IsValidPtr(Address) && IsAttached)
            {
                byte[] buffer = new byte[8];
                buffer = BitConverter.GetBytes(Value);
                WriteBytes(Address, buffer, ProtectionBypass);
            }
        }

        public void WriteFloat(ulong Address, float Value, bool ProtectionBypass = false)
        {
            if (IsValidPtr(Address) && IsAttached)
            {
                byte[] buffer = new byte[4];
                buffer = BitConverter.GetBytes(Value);
                WriteBytes(Address, buffer, ProtectionBypass);
            }
        }

        public void WriteDouble(ulong Address, double Value, bool ProtectionBypass = false)
        {
            if (IsValidPtr(Address) && IsAttached)
            {
                byte[] buffer = new byte[4];
                buffer = BitConverter.GetBytes(Value);
                WriteBytes(Address, buffer, ProtectionBypass);
            }
        }

        public void WriteString(ulong Address, string text, bool Unicode = false, bool ProtectionBypass = false)
        {

            if (IsValidPtr(Address) && IsAttached)
            {
                if (Unicode)
                {
                    var enc = new UnicodeEncoding();
                    WriteBytes(Address, enc.GetBytes(text), ProtectionBypass);
                    WriteByte(Address + (ulong)(text.Length * 2), 0, ProtectionBypass);
                }
                else
                {
                    var enc = new ASCIIEncoding();
                    WriteBytes(Address, enc.GetBytes(text), ProtectionBypass);
                    WriteByte(Address + (ulong)(text.Length), 0, ProtectionBypass);
                }

            }
        }

        #region OPCODE Shortcuts
        public void OP_True(ulong address, bool ProtectionBypass = false)
        {
            WriteBytes(address, new byte[] { 0x55, 0x48, 0x8B, 0xEC, 0xB8, 0x01, 0x00, 0x00, 0x00, 0x5D, 0xC3 }, ProtectionBypass);
        }
        public void OP_False(ulong address, bool ProtectionBypass = false)
        {
            WriteBytes(address, new byte[] { 0x55, 0x48, 0x8B, 0xEC, 0xB8, 0x00, 0x00, 0x00, 0x00, 0x5D, 0xC3 }, ProtectionBypass);
        }
        #endregion

        #endregion        

        #endregion

        #region Pattern Scanning

        // for comparing a region in memory, needed in finding a signature
        public bool MemoryCompare(byte[] Data, Int32 DataOffset, byte[] Pattern, string WildCards)
        {
            for (int i = 0; i <= Pattern.Length - 1; i++)
            {
                if (WildCards[i].ToString() == "x" && Data[DataOffset] != Pattern[i])
                {
                    return false;
                }
            }

            return true;
        }

        public ulong PatternScan(ulong StartAddress, Int64 Size, byte[] Pattern, string WildCards, int ScanAlignment = 1)
        {
            if (WildCards.Length != Pattern.Length)
            {
                Debug.WriteLine("Mask is wrong size!");
                return 0;
            }

            if (IsValidPtr(StartAddress) && IsAttached)
            {
                int ChunkSize = 8; //lowers possible scan times but increases chance of missing a value if its partialy in 2 chunks
                if (Size > Int32.MaxValue / ChunkSize)
                {
                    int blocks = (Int32)(Size / (Int32.MaxValue / ChunkSize));
                    ulong BlockSize = (ulong)(Int32.MaxValue / ChunkSize) + 1;
                    Debug.WriteLine("Total Blocks : " + blocks.ToString());
                    Debug.WriteLine("Size Of Each Block : " + BlockSize.ToString());

                    for (int b = 0; b <= blocks; b++)
                    {
                        Debug.WriteLine("Starting Block: " + b.ToString());
                        Debug.WriteLine("Start Address Of Block : " + ((StartAddress + (BlockSize * (ulong)b))).ToString("X"));

                        byte[] buffer = new byte[BlockSize + (ulong)Pattern.Length];
                        Debug.WriteLine("buffer size: " + buffer.Length.ToString());

                        buffer = ReadBytes(StartAddress + (BlockSize * (ulong)b), (Int32)(BlockSize + (ulong)Pattern.Length));
                        Debug.WriteLine("Finished Reading Buffer!");

                        if (buffer != null)
                        {
                            for (int i = 0; i <= (Int32)BlockSize; i++)
                            {
                                for (int p = 0; p <= Pattern.Length - 1; p++)
                                {
                                    if (WildCards[p].ToString() == "x" && buffer[i + p] != Pattern[p])
                                    {
                                        break;
                                    }
                                    if (p == Pattern.Length - 1)
                                    {
                                        return (StartAddress + (ulong)(BlockSize * (ulong)b)) + (ulong)i;
                                    }
                                }
                                i += ScanAlignment - 1;
                            }
                        }
                        else
                        {
                            //buffer was null
                            return 0;
                        }
                    }
                    //no results found
                    return 0;
                }
                else
                {
                    byte[] buffer = new byte[Size];
                    buffer = ReadBytes(StartAddress, (Int32)Size);

                    if (buffer != null)
                    {
                        for (int i = 0; i <= Size; i++)
                        {
                            for (int p = 0; p <= Pattern.Length - 1; p++)
                            {
                                if (WildCards[p].ToString() == "x" && buffer[i + p] != Pattern[p])
                                {
                                    break;
                                }
                                if (p == Pattern.Length - 1)
                                {
                                    return StartAddress + +(ulong)i;
                                }
                            }
                            //i += ScanAlignment - 1;
                        }
                        //no results found
                        return 0;
                    }
                    else
                    {
                        //buffer was null
                        return 0;
                    }
                }
            }
            else
            {
                //addr wrong or not attatched to a game
                return 0;
            }

        }
        #endregion
    }
}
