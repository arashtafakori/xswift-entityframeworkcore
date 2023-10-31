using Microsoft.Extensions.Configuration;

namespace EntityFrameworkCore.XSwift
{
    public class SqlServerSettings
    {
 
        private IConfigurationRoot? _configuration;

        public string? _connectString;
        public string ConnectString
        {
            get => _connectString!;
        }

        public SqlServerSettings()
        {

        }
        public SqlServerSettings(IConfigurationRoot configuration)
        {
            _configuration = configuration;

            _connectString = _configuration
                .GetSection("SqlServerSettings")
                .GetSection("ConnectionStrings").Value!;
        }

        public void SetSqlServerConnectString(string value)
        {
            _connectString = value;
        }
    }
}
