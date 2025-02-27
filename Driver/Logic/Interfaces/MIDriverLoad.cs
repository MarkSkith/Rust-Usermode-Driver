﻿namespace MDriver.Logic.Interfaces
{
    internal interface MIDriverLoad
    {
        /// <summary>
        /// Gets a value indicating whether this driver is created.
        /// </summary>
        bool IsCreated
        {
            get;
        }

        /// <summary>
        /// Gets a value indicating whether this driver is loaded.
        /// </summary>
        bool IsLoaded
        {
            get;
        }

        /// <summary>
        /// Creates the specified driver.
        /// </summary>
        /// <param name="Driver">The driver.</param>
        bool CreateDriver(MDriver Driver);

        /// <summary>
        /// Loads the specified driver.
        /// </summary>
        bool LoadDriver();

        /// <summary>
        /// Stops the specified driver.
        /// </summary>
        bool StopDriver();

        /// <summary>
        /// Deletes the specified driver.
        /// </summary>
        bool DeleteDriver();
    }
}
