namespace OJS.Workers.ExecutionStrategies.Sql.MySql
{
    using System.Data;
    using System.Globalization;
    using System.Text.RegularExpressions;
    using global::MySql.Data.MySqlClient;

    public abstract class BaseMySqlExecutionStrategy : BaseSqlExecutionStrategy
    {
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        private const string TimeSpanFormat = "HH:mm:ss";

        protected BaseMySqlExecutionStrategy(
            string sysDbConnectionString,
            string restrictedUserId,
            string restrictedUserPassword)
            : base(sysDbConnectionString, restrictedUserId, restrictedUserPassword)
        {
        }

        public override IDbConnection GetOpenConnection(string databaseName)
        {
            using (var connection = new MySqlConnection(this.MasterDbConnectionString))
            {
                connection.Open();

                var createDatabaseQuery = $"CREATE DATABASE `{databaseName}`;";

                var createUserQuery = $@"
                    CREATE USER IF NOT EXISTS '{this.RestrictedUserId}'@'%';
                    ALTER USER '{this.RestrictedUserId}' IDENTIFIED BY '{this.RestrictedUserPassword}'";
                    /* SET PASSWORD FOR '{this.restrictedUserId}'@'%'=PASSWORD('{this.restrictedUserPassword}')"; */

                var grandPrivilegesToUserQuery = $@"
                    GRANT ALL PRIVILEGES ON `{databaseName}`.* TO '{this.RestrictedUserId}'@'%';
                    FLUSH PRIVILEGES;";

                var enableLogBinTrustFunctionCreatorsQuery = "SET GLOBAL log_bin_trust_function_creators = 1;";

                this.ExecuteNonQuery(connection, createDatabaseQuery);
                this.ExecuteNonQuery(connection, createUserQuery);
                this.ExecuteNonQuery(connection, grandPrivilegesToUserQuery);
                this.ExecuteNonQuery(connection, enableLogBinTrustFunctionCreatorsQuery);
            }

            var workerConnection = new MySqlConnection(this.BuildWorkerDbConnectionString(databaseName));
            workerConnection.Open();

            return workerConnection;
        }

        public override void DropDatabase(string databaseName)
        {
            using (var connection = new MySqlConnection(this.MasterDbConnectionString))
            {
                connection.Open();

                this.ExecuteNonQuery(connection, $"DROP DATABASE IF EXISTS `{databaseName}`;");
            }
        }

        protected override string GetDataRecordFieldValue(IDataRecord dataRecord, int index)
        {
            if (!dataRecord.IsDBNull(index))
            {
                var fieldType = dataRecord.GetFieldType(index);

                if (fieldType == DateTimeType)
                {
                    return dataRecord.GetDateTime(index).ToString(DateTimeFormat, CultureInfo.InvariantCulture);
                }

                if (fieldType == TimeSpanType)
                {
                    return ((MySqlDataReader)dataRecord)
                        .GetTimeSpan(index)
                        .ToString(TimeSpanFormat, CultureInfo.InvariantCulture);
                }
            }

            return base.GetDataRecordFieldValue(dataRecord, index);
        }

        protected override string BuildWorkerDbConnectionString(string databaseName)
        {
            var userIdRegex = new Regex("UID=.*?;");
            var passwordRegex = new Regex("Password=.*?;");

            var workerDbConnectionString = this.MasterDbConnectionString;

            workerDbConnectionString =
                userIdRegex.Replace(workerDbConnectionString, $"UID={this.RestrictedUserId};");

            workerDbConnectionString =
                passwordRegex.Replace(workerDbConnectionString, $"Password={this.RestrictedUserPassword}");

            workerDbConnectionString += $";Database={databaseName};Pooling=False;";

            return workerDbConnectionString;
        }
    }
}