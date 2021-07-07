namespace MDriver.Logic
{
    using System;

    using global::MDriver.Logic.Enums;
    using global::MDriver.Logic.Interfaces;
    using global::MDriver.Logic.Loaders;
    using global::MDriver.Utilities;

    public partial class MDriver : MIDriver
    {
        /// <summary>
        /// Gets or sets the IO requests handler.
        /// </summary>
        public MIDriverIo IO
        {
            get;
        }

        /// <summary>
        /// Gets or sets the driver loader.
        /// </summary>
        internal MIDriverLoad Loader
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        public MDriverConfig Config
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the load event.
        /// </summary>
        public EventHandler Loaded
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the unload event.
        /// </summary>
        public EventHandler Unloaded
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the disposed event.
        /// </summary>
        public EventHandler Disposed
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="MDriver"/> is loaded.
        /// </summary>
        public bool IsLoaded
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="MDriver"/> is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get;
            private set;
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="MDriver"/> class from being created.
        /// </summary>
        protected MDriver()
        {
            // Driver.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MDriver"/> class.
        /// </summary>
        /// <param name="Config">The configuration.</param>
        /// <param name="LoaderPath">The path of the driver loader.</param>
        public MDriver(MDriverConfig Config, string LoaderPath = null)
        {
            this.Setup(Config, LoaderPath);

            switch (Config.IoMethod)
            {
                case MIoMethod.None:
                {
                    this.IO = null;
                    break;
                }

                case MIoMethod.IoControl:
                {
                    this.IO = new MDriverIo(this);
                    break;
                }

                case MIoMethod.SharedMemory:
                {
                    this.IO = new MDriverIoShared(this);
                    break;
                }

                default:
                {
                    throw new ArgumentException("Invalid IoMethod specified", nameof(Config.IoMethod));
                }
            }
        }

        /// <summary>
        /// Setups the specified driver.
        /// </summary>
        /// <param name="Config">The driver configuration.</param>
        /// <param name="LoaderPath">The path of the driver loader.</param>
        /// <exception cref="ArgumentException">Invalid LoadType specified.</exception>
        public void Setup(MDriverConfig Config, string LoaderPath = null)
        {
            if (Config == null)
            {
                throw new ArgumentNullException(nameof(Config));
            }

            this.Config = Config;

            if (string.IsNullOrEmpty(Config.ServiceName))
            {
                throw new Exception("Config->ServiceName is null or empty");
            }

            if (Config.IoMethod == MIoMethod.IoControl)
            {
                if (string.IsNullOrEmpty(Config.SymbolicLink))
                {
                    throw new Exception("Config->SymbolicLink is null or empty");
                }
            }

            if (!string.IsNullOrEmpty(LoaderPath))
            {
                this.SetLoaderPath(LoaderPath);
            }

            switch (this.Config.LoadMethod)
            {
                case MDriverLoad.Normal:
                {
                    this.Loader = new MServiceLoad();
                    break;
                }              

                default:
                {
                    throw new ArgumentException("Invalid LoadMethod specified", nameof(Config.LoadMethod));
                }
            }
        }

        /// <summary>
        /// Sets the loader path.
        /// </summary>
        /// <param name="Path">The path.</param>
        public void SetLoaderPath(string Path)
        {
            if (string.IsNullOrEmpty(Path))
            {
                throw new ArgumentNullException(nameof(Path));
            }

            switch (this.Config.LoadMethod)
            {
                case MDriverLoad.Normal:
                {
                    break;
                }                

                default:
                {
                    throw new ArgumentException("Invalid LoadMethod specified", nameof(this.Config.LoadMethod));
                }
            }
        }

        /// <summary>
        /// Loads the specified driver/system file.
        /// </summary>
        public bool Load()
        {
            if (!this.Loader.CreateDriver(this))
            {
                Log.Error(typeof(MDriver), "Failed to create the driver at Load().");
                return false;
            }
            if (!MDriver.CanConnectTo(this.Config.SymbolicLink))
            {
                if (!this.Loader.LoadDriver())
                {
                    Log.Error(typeof(MDriver), "Failed to load the driver at Load().");
                    return false;
                }
            }
            else
            {
                Log.Warning(typeof(MDriver), "Warning, driver already exist at Load().");
            }
            this.IsLoaded = true;
            if (this.IO.IsConnected)
            {
                this.IO.Disconnect();
            }
            this.IO.Connect();
            if (!this.IO.IsConnected)
            {
                Log.Error(typeof(MDriver), "Failed to open the symbolic file.");
            }
            if (this.Loaded != null)
            {
                try
                {
                    this.Loaded(this, EventArgs.Empty);
                }
                catch (Exception)
                {
                }
            }
            return true;
        }

        /// <summary>
        /// Unloads the currently loaded driver/system file.
        /// </summary>
        public bool Unload()
        {
            if (this.IO.IsConnected)
            {
                this.IO.Disconnect();
            }
            if (!this.Loader.StopDriver())
            {
                Log.Error(typeof(MDriver), "Failed to unload the driver at Unload().");
                return false;
            }
            if (!this.Loader.DeleteDriver())
            {
                Log.Error(typeof(MDriver), "Failed to delete the driver at Unload().");
                return false;
            }
            this.IsLoaded = false;
            if (this.Unloaded != null)
            {
                try
                {
                    this.Unloaded(this, EventArgs.Empty);
                }
                catch (Exception)
                {
                }
            }
            return true;
        }

        /// <summary>
        /// Exécute les tâches définies par l'application associées
        /// à la libération ou à la redéfinition des ressources non managées.
        /// </summary>
        public void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.IsDisposed = true;

            // ...

            try
            {
                if (!this.Unload())
                {
                    Log.Error(typeof(MDriver), "Failed to unload the driver at Dispose().");
                }
            }
            catch (Exception Exception)
            {
                Log.Error(typeof(MDriver), Exception.GetType().Name + ", " + Exception.Message);
            }

            // ..

            this.IO?.Dispose();

            // ..

            if (this.Disposed != null)
            {
                try
                {
                    this.Disposed.Invoke(this, EventArgs.Empty);
                }
                catch (Exception)
                {
                    // ...
                }
            }
        }
    }
}