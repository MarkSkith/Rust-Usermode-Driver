﻿namespace MDriver.Logic.Interfaces
{
    using System;

    using Microsoft.Win32.SafeHandles;

    public interface MIDriverIo : IDisposable
    {
        /// <summary>
        /// Gets or sets the <see cref="MIDriver"/>.
        /// </summary>
        MIDriver Driver
        {
            get;
        }

        /// <summary>
        /// Gets or sets the safe IO file handle.
        /// </summary>
        SafeFileHandle Handle
        {
            get;
        }

        /// <summary>
        /// Gets or sets the connected event.
        /// </summary>
        EventHandler Connected
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the disconnected event.
        /// </summary>
        EventHandler Disconnected
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the disposed event.
        /// </summary>
        EventHandler Disposed
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="MIDriver"/> is connected.
        /// </summary>
        bool IsConnected
        {
            get;
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="MIDriver"/> is disposed.
        /// </summary>
        bool IsDisposed
        {
            get;
        }

        /// <summary>
        /// Connects this instance to the driver.
        /// </summary>
        void Connect();

        /// <summary>
        /// Disconnects this instance from the driver.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Tries to execute a DeviceIoControl request against the driver.
        /// </summary>
        /// <param name="IoCtl">The IO request control code.</param>
        bool TryIoControl(uint IoCtl);

        /// <summary>
        /// Tries to execute a DeviceIoControl request against the driver.
        /// </summary>
        /// <param name="IoCtl">The IO request control code.</param>
        /// <param name="IoData">The IO request data.</param>
        bool TryIoControl<TInput>(uint IoCtl, TInput IoData);

        /// <summary>
        /// Tries to execute a DeviceIoControl request against the driver.
        /// </summary>
        /// <param name="IoCtl">The IO request control code.</param>
        /// <param name="IoData">The IO request data.</param>
        /// <param name="IoDataSize">The IO request data size.</param>
        bool TryIoControl<TInput>(uint IoCtl, TInput IoData, int IoDataSize);

        /// <summary>
        /// Tries to execute a DeviceIoControl request against the driver.
        /// </summary>
        /// <param name="IoCtl">The IO request control code.</param>
        /// <param name="IoData">The IO request data.</param>
        /// <param name="IoOutput">The IO request output data.</param>
        bool TryIoControl<TInput, TOutput>(uint IoCtl, TInput IoData, out TOutput IoOutput);

        /// <summary>
        /// Tries to execute a DeviceIoControl request against the driver.
        /// </summary>
        /// <param name="IoCtl">The IO request control code.</param>
        /// <param name="IoData">The IO request data.</param>
        /// <param name="IoDataSize">The IO request data size.</param>
        bool TryIoControl<TInput, TOutput>(uint IoCtl, TInput IoData, int IoDataSize, out TOutput IoOutput, int IoOutputSize);
    }
}
