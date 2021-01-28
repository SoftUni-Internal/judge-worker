namespace OJS.Workers.ExecutionStrategies.Sql.MySql
{
    using System;
    using System.Data;
    using System.Globalization;
    using System.Text.RegularExpressions;
    using global::MySql.Data.MySqlClient;

    public abstract class BaseMySqlExecutionStrategy : BaseSqlExecutionStrategy
    {
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        private const string TimeSpanFormat = "HH:mm:ss";

        private readonly string sysDbConnectionString;
        private readonly string restrictedUserId;
        private readonly string restrictedUserPassword;

        protected BaseMySqlExecutionStrategy(
            string sysDbConnectionString,
            string restrictedUserId,
            string restrictedUserPassword)
        {
            if (string.IsNullOrWhiteSpace(sysDbConnectionString))
            {
                throw new ArgumentException("Invalid sys DB connection string!", nameof(sysDbConnectionString));
            }

            if (string.IsNullOrWhiteSpace(restrictedUserId))
            {
                throw new ArgumentException("Invalid restricted user ID!", nameof(restrictedUserId));
            }

            if (string.IsNullOrWhiteSpace(restrictedUserPassword))
            {
                throw new ArgumentException("Invalid restricted user password!", nameof(restrictedUserPassword));
            }

            this.sysDbConnectionString = sysDbConnectionString;
            this.restrictedUserId = restrictedUserId;
            this.restrictedUserPassword = restrictedUserPassword;
        }

        public override IDbConnection GetOpenConnection(string databaseName)
        {
            using (var connection = new MySqlConnection(this.sysDbConnectionString))
            {
                connection.Open();

                var createDatabaseQuery = $"CREATE DATABASE `{databaseName}`;";

                var createUserQuery = $@"
                    CREATE USER IF NOT EXISTS '{this.restrictedUserId}'@'%';
                    ALTER USER '{this.restrictedUserId}' IDENTIFIED BY '{this.restrictedUserPassword}'";
                    /* SET PASSWORD FOR '{this.restrictedUserId}'@'%'=PASSWORD('{this.restrictedUserPassword}')"; */

                var grandPrivilegesToUserQuery = $@"
                    GRANT ALL PRIVILEGES ON `{databaseName}`.* TO '{this.restrictedUserId}'@'%';
                    FLUSH PRIVILEGES;";

                this.ExecuteNonQuery(connection, createDatabaseQuery);

                this.ExecuteNonQuery(connection, createUserQuery);

                this.ExecuteNonQuery(connection, grandPrivilegesToUserQuery);
            }

            var userIdRegex = new Regex("UID=.*?;");
            var passwordRegex = new Regex("Password=.*?;");

            var createdDbConnectionString = this.sysDbConnectionString;

            createdDbConnectionString =
                userIdRegex.Replace(createdDbConnectionString, $"UID={this.restrictedUserId};");

            createdDbConnectionString =
                passwordRegex.Replace(createdDbConnectionString, $"Password={this.restrictedUserPassword}");

            createdDbConnectionString += $";Database={databaseName};Pooling=False;";

            var createdDbConnection = new MySqlConnection(createdDbConnectionString);
            createdDbConnection.Open();

            return createdDbConnection;
        }

        public override void DropDatabase(string databaseName)
        {
            using (var connection = new MySqlConnection(this.sysDbConnectionString))
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
    }
}