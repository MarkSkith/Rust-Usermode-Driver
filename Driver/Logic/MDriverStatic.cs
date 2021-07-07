namespace MDriver.Logic
{
    using System;
    using System.IO;

    using global::MDriver.Logic.Enums;
    using global::MDriver.Logic.Interfaces;
    using global::MDriver.Native;

    public partial class MDriver : MIDriver
    {
        /// <summary>
        /// Checks if the specified symbolic file exists.
        /// </summary>
        /// <param name="SymbolicName">Path of the symbolic file.</param>
        public static bool CanConnectTo(string SymbolicName, MIoMethod IoMethod = MIoMethod.IoControl)
        {
            switch (IoMethod)
            {
                case MIoMethod.IoControl:
                {
                    var Handle = WinApi.CreateFile(SymbolicName, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);
                    var Exists = (Handle != null && !Handle.IsInvalid);

                    if (Handle != null)
                    {
                        Handle.Close();
                    }

                    return Exists;
                }

                case MIoMethod.SharedMemory:
                {
                    break;
                }

                default:
                {
                    throw new ArgumentException();
                }
            }

            return false;
        }
    }
}