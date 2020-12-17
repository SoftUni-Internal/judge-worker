namespace OJS.Workers.ExecutionStrategies.Sql.MariaDb
{
    using System;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using global::MySql.Data.MySqlClient;

    public abstract class BaseMariaDbSingleDatabaseExecutionStrategy : BaseSqlExecutionStrategy
    {
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        private const string TimeSpanFormat = "HH:mm:ss";

        private readonly string sysDbConnectionString;
        private readonly string restrictedUserId;
        private readonly string restrictedUserPassword;

        private static readonly string DatabaseName = $"testing_{Guid.NewGuid()}";

        protected BaseMariaDbSingleDatabaseExecutionStrategy(
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

        public string RestrictedUserId => $"{this.GetDatabaseName()}_{this.restrictedUserId}";

        private void EnsureDatabaseIsSetup()
        {
            var databaseName = this.GetDatabaseName();

            using (var connection = new MySqlConnection(this.sysDbConnectionString))
            {
                connection.Open();

                var createDatabaseQuery = $"CREATE DATABASE IF NOT EXISTS `{databaseName}`;";

                var createUserQuery = $@"
                    CREATE USER IF NOT EXISTS '{this.RestrictedUserId}'@'%';
                    SET PASSWORD FOR '{this.RestrictedUserId}'@'%'=PASSWORD('{this.restrictedUserPassword}')";

                var grandPrivilegesToUserQuery = $@"
                    GRANT ALL PRIVILEGES ON `{databaseName}`.* TO '{this.RestrictedUserId}'@'%';
                    FLUSH PRIVILEGES;";

                this.ExecuteNonQuery(connection, createDatabaseQuery);
                this.ExecuteNonQuery(connection, createUserQuery);
                this.ExecuteNonQuery(connection, grandPrivilegesToUserQuery);
            }

            this.WorkerDbConnectionString =
                $"Server=mariadb;UID={this.RestrictedUserId};Password={this.restrictedUserPassword};Database={databaseName};Pooling=False;";
        }

        public override IDbConnection GetOpenConnection(string databaseName)
        {
            this.EnsureDatabaseIsSetup();

            var createdDbConnection = new MySqlConnection(this.WorkerDbConnectionString);
            createdDbConnection.Open();
            return createdDbConnection;
        }

        public override string GetDatabaseName() => DatabaseName;

        public string WorkerDbConnectionString { get; set; }

        public override void DropDatabase(string databaseName)
        {
            var connectionString = $"{this.sysDbConnectionString};Database={databaseName}";
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                var dropAlLQuery = string.Join("\n",
                    this.BuildDropTablesQuery(connection, databaseName),
                    this.BuildDropStoredProceduresAndFunctionsQuery(connection, databaseName),
                    this.BuildDropViewsQuery(connection, databaseName));
                this.ExecuteNonQuery(connection, dropAlLQuery);
            }
        }

        private string BuildDropItemsByTypeQuery(IDbConnection connection, string databaseName, string type)
        {
            var getNamesQuery = $@"
SELECT table_name
FROM information_schema.{type}s
WHERE table_schema = '{databaseName}';";

            var dropAllQuery = string.Join(
                ";\n",
                this.ExecuteReader(connection, getNamesQuery)
                    .Results
                    .Select(tableName => $"DROP {type} {tableName}"));

            return
                string.IsNullOrWhiteSpace(dropAllQuery)
                    ? string.Empty
                    : $@"
SET FOREIGN_KEY_CHECKS=0;
{dropAllQuery};
SET FOREIGN_KEY_CHECKS=1;";
        }

        private string BuildDropTablesQuery(IDbConnection connection, string databaseName)
            => this.BuildDropItemsByTypeQuery(connection, databaseName, "table");
        private string BuildDropViewsQuery(IDbConnection connection, string databaseName)
            => this.BuildDropItemsByTypeQuery(connection, databaseName, "view");

        private string BuildDropStoredProceduresAndFunctionsQuery(IDbConnection connection, string databaseName)
            => $"DELETE FROM mysql.proc WHERE db LIKE '{databaseName}'";

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
