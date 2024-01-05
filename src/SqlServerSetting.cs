using Microsoft.Extensions.Configuration;

namespace XSwift.EntityFrameworkCore
{
    /// <summary>
    /// Represents a class for managing SQL Server connection settings.
    /// </summary>
    public class SqlServerSetting
    {
 
        private IConfigurationRoot? _configuration;

        public string? _connectString;

        /// <summary>
        /// Gets the SQL Server connection string.
        /// </summary>
        public string ConnectString
        {
            get => _connectString!;
        }

        /// <summary>
        /// Default constructor for SqlServerSetting.
        /// </summary>
        public SqlServerSetting()
        {
        }

        /// <summary>
        /// Constructor for SqlServerSetting that takes an IConfigurationRoot object.
        /// </summary>
        /// <param name="configuration">An IConfigurationRoot object containing SQL Server settings.</param>
        /// 
        public SqlServerSetting(IConfigurationRoot configuration)
        {
            _configuration = configuration;

            var SqlServerSetting = _configuration
                .GetSection("SqlServerSetting");

            var mode = SqlServerSetting.GetSection("Mode").Value;

            if(mode == "WindowsAuthentication")
            {
                var windowsAuthentication = SqlServerSetting.GetSection("WindowsAuthentication");

                _connectString = $"Server={windowsAuthentication.GetSection("Server").Value};Database={windowsAuthentication.GetSection("Database").Value};Trusted_Connection=True";
            }
            else if (mode == "SqlServerAuthentication")
            {
                var sqlServerAuthentication = SqlServerSetting.GetSection("SqlServerAuthentication");

                _connectString = $"Data Source={sqlServerAuthentication.GetSection("Server").Value};" +
                    $"Initial Catalog={sqlServerAuthentication.GetSection("Database").Value};" +
                    $"User ID={sqlServerAuthentication.GetSection("User").Value};" +
                    $"Password={sqlServerAuthentication.GetSection("Password").Value}";
            }
            else if (mode == "ConnectionString")
            {
                _connectString = SqlServerSetting.GetSection("ConnectionString").Value;
            }
        }

        /// <summary>
        /// Sets the SQL Server connection string.
        /// </summary>
        /// <param name="value">The new connection string value.</param>
        public void SetSqlServerConnectString(string value)
        {
            _connectString = value;
        }
    }
}
