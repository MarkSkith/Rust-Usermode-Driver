namespace MDriver.Logic.Loaders
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Security.Permissions;
    using System.ServiceProcess;

    using global::MDriver.Logic.Interfaces;
    using global::MDriver.Native.Enums.Services;

    using ServiceType       = global::MDriver.Native.Enums.Services.ServiceType;
    using TimeoutException  = System.TimeoutException;

    [ServiceControllerPermission(SecurityAction.Demand, PermissionAccess = ServiceControllerPermissionAccess.Control)]
    internal sealed class MServiceLoad : MIDriverLoad
    {
        /// <summary>
        /// Gets a value indicating whether this driver is created.
        /// </summary>
        public bool IsCreated
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether this driver is loaded.
        /// </summary>
        public bool IsLoaded
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the driver.
        /// </summary>
        public MDriver Driver
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service handle.
        /// </summary>
        public IntPtr ServiceHandle
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service associated to this driver.
        /// </summary>
        public ServiceController Service
        {
            get;
            private set;
        }

        /// <summary>
        /// Creates the specified driver.
        /// </summary>
        public bool CreateDriver(MDriver Driver)
        {
            var Config = Driver.Config;

            if (this.IsCreated)
            {
                throw new Exception("Service is already created");
            }

            if (Config == null)
            {
                throw new ArgumentNullException(nameof(Config));
            }

            this.Driver = Driver;

            if (Driver == null)
            {
                throw new ArgumentNullException(nameof(Driver), "Driver is null");
            }

            this.ServiceHandle = Utilities.MService.CreateOrOpen(Config.ServiceName, Config.ServiceName, ServiceAccess.ServiceAllAccess, ServiceType.ServiceKernelDriver, ServiceStart.ServiceDemandStart, ServiceError.ServiceErrorNormal, Config.DriverFile);

            if (this.ServiceHandle == IntPtr.Zero)
            {
                return false;
            }

            this.Service = new ServiceController(Config.ServiceName);
            //Debug.WriteLine("[*] Service->Status      : " +  this.Service.Status);
            //if (this.Service.Status != ServiceControllerStatus.Stopped && this.Service.CanStop)
            //{
            //    Console.WriteLine("About To Stop The Service.");
            //    try
            //    {
            //        this.Service.Stop();
            //    }
            //    catch (Exception Exception)
            //    {
            //        Console.WriteLine("Fail Stop 1.");
            //        Log.Error(typeof(MServiceLoad), Exception.GetType().Name + ", " + Exception.Message);
            //        return false;
            //    }
            //
            //    try
            //    {
            //        this.Service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
            //    }
            //    catch (Exception Exception)
            //    {
            //        Console.WriteLine("Fail Stop 2.");
            //        Log.Error(typeof(MServiceLoad), Exception.GetType().Name + ", " + Exception.Message);
            //        return false;
            //    }
            //}

            this.IsCreated = true;
            return true;
        }

        /// <summary>
        /// Loads the specified driver.
        /// </summary>
        public bool LoadDriver()
        {
            if (!this.IsCreated)
            {
                throw new Exception("Service is not created.");
            }

            if (this.IsLoaded)
            {
                return true;
            }

            if (this.Service.Status != ServiceControllerStatus.Running)
            {
                try
                {
                    this.Service.Start();
                }
                catch (InvalidOperationException Exception)
                {
                    Log.Error(typeof(MServiceLoad), Exception.GetType().Name + ", " + Exception.Message);
                    return false;
                }
                catch (Win32Exception Exception)
                {
                    if (Exception.Message.Contains("signature"))
                    {
                        Log.Error(typeof(MServiceLoad), "The driver is not signed, unable to load it using the service manager.");
                    }
                    else
                    {
                        Log.Error(typeof(MServiceLoad), Exception.GetType().Name + ", " + Exception.Message);
                    }

                    return false;
                }
                catch (Exception Exception)
                {
                    Log.Error(typeof(MServiceLoad), Exception.GetType().Name + ", " + Exception.Message);
                    return false;
                }

                try
                {
                    this.Service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                }
                catch (TimeoutException)
                {
                    Log.Error(typeof(MServiceLoad), "Failed to start the service in 10 seconds.");
                }
                catch (Exception Exception)
                {
                    Log.Error(typeof(MServiceLoad), Exception.GetType().Name + ", " + Exception.Message);
                    return false;
                }
            }

            this.IsLoaded = true;

            return true;
        }

        /// <summary>
        /// Stops the specified driver.
        /// </summary>
        public bool StopDriver()
        {
            if (!this.IsCreated)
            {
                throw new Exception("Service is not created.");
            }

            if (!this.IsLoaded)
            {
                return true;
            }

            if (this.Service.CanStop)
            {
                try
                {
                    this.Service.Stop();
                }
                catch (Exception Exception)
                {
                    Log.Error(typeof(MServiceLoad), Exception.GetType().Name + ", " + Exception.Message);
                    return false;
                }

                this.IsLoaded = false;

                try
                {
                    this.Service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                }
                catch (TimeoutException)
                {
                    Log.Error(typeof(MServiceLoad), "Failed to stop the service in 10 seconds.");
                }
                catch (Exception Exception)
                {
                    Log.Error(typeof(MServiceLoad), Exception.GetType().Name + ", " + Exception.Message);
                    return false;
                }
            }
            else
            {
                Log.Error(typeof(MServiceLoad), "Driver not stopped !");
                return false;
            }

            this.IsLoaded = false;

            return true;
        }

        /// <summary>
        /// Deletes the specified driver.
        /// </summary>
        public bool DeleteDriver()
        {
            if (!this.IsCreated)
            {
                throw new Exception("Service is not created.");
            }

            if (this.IsLoaded)
            {
                if (!this.StopDriver())
                {
                    return false;
                }
            }

            if (this.Service != null)
            {
                this.Service.Dispose();
            }

            if (this.ServiceHandle != IntPtr.Zero)
            {
                if (!Utilities.MService.Delete(this.ServiceHandle))
                {
                    Log.Error(typeof(MServiceLoad), "Unable to delete the service using the native api.");
                }

                this.ServiceHandle = IntPtr.Zero;
            }

            this.IsCreated  = false;

            return true;
        }
    }
}
