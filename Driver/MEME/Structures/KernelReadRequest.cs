using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDriver.MEME.Structures
{
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct KernelReadRequest
    {
        /// <summary>
        /// The process identifier.
        /// </summary>
        internal ulong ProcessId;

        /// <summary>
        /// The address/offset of we are reading at.
        /// </summary>
        internal ulong Address;

        /// <summary>
        /// The buffer containing the response.
        /// </summary>
        internal ulong Response;

        /// <summary>
        /// The size of the buffer.
        /// </summary>
        internal Int32 Size;
    }
}
