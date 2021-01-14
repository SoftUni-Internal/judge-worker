namespace OJS.Workers.ExecutionStrategies.Sql.SqlServerSingleDatabase
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Globalization;
    using System.Transactions;

    public abstract class BaseSqlServerSingleDatabaseExecutionStrategy : BaseSqlExecutionStrategy
    {
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
        private const string DateTimeOffsetFormat = "yyyy-MM-dd HH:mm:ss.fffffff zzz";
        private const string TimeSpanFormat = "HH:mm:ss.fffffff";

        private static readonly Type DateTimeOffsetType = typeof(DateTimeOffset);
        private static string databaseName = Guid.NewGuid().ToString();

        private readonly string masterDbConnectionString;
        private readonly string restrictedUserId;
        private readonly string restrictedUserPassword;
        private TransactionScope transactionScope;

        protected BaseSqlServerSingleDatabaseExecutionStrategy(
            string masterDbConnectionString,
            string restrictedUserId,
            string restrictedUserPassword)
        {
            if (string.IsNullOrWhiteSpace(masterDbConnectionString))
            {
                throw new ArgumentException("Invalid master DB connection string!", nameof(masterDbConnectionString));
            }

            if (string.IsNullOrWhiteSpace(restrictedUserId))
            {
                throw new ArgumentException("Invalid restricted user ID!", nameof(restrictedUserId));
            }

            if (string.IsNullOrWhiteSpace(restrictedUserPassword))
            {
                throw new ArgumentException("Invalid restricted user password!", nameof(restrictedUserPassword));
            }

            this.masterDbConnectionString = masterDbConnectionString;
            this.restrictedUserId = restrictedUserId;
            this.restrictedUserPassword = restrictedUserPassword;
            this.transactionScope = new TransactionScope();
        }

        public override IDbConnection GetOpenConnection(string databaseName)
        {
            var databaseFilePath = $"{this.WorkingDirectory}\\{databaseName}.mdf";

            using (var connection = new SqlConnection(this.masterDbConnectionString))
            {
                connection.Open();

                var createDatabaseQuery =
                    $@"IF DB_ID('{databaseName}') IS NOT NULL
                    BEGIN
                    CREATE DATABASE [{databaseName}] ON PRIMARY (NAME=N'{databaseName}', FILENAME=N'{databaseFilePath}');
                    END;";

                var createLoginQuery = $@"
                    IF NOT EXISTS (SELECT name FROM master.sys.server_principals WHERE name=N'{this.restrictedUserId}')
                    BEGIN
                    CREATE LOGIN [{this.restrictedUserId}] WITH PASSWORD=N'{this.restrictedUserPassword}',
                    DEFAULT_DATABASE=[master],
                    DEFAULT_LANGUAGE=[us_english],
                    CHECK_EXPIRATION=OFF,
                    CHECK_POLICY=ON;
                    END;";

                var createUserAsDbOwnerQuery = $@"
                    USE [{databaseName}];
                    CREATE USER [{this.restrictedUserId}] FOR LOGIN [{this.restrictedUserId}];
                    ALTER ROLE [db_owner] ADD MEMBER [{this.restrictedUserId}];";

                this.ExecuteNonQuery(connection, createDatabaseQuery);

                this.ExecuteNonQuery(connection, createLoginQuery);

                this.ExecuteNonQuery(connection, createUserAsDbOwnerQuery);
            }

            var createdDbConnectionString =
                $"Data Source=.,3001;User Id={this.restrictedUserId};Password={this.restrictedUserPassword};AttachDbFilename={databaseFilePath};Pooling=False;";
            this.transactionScope = new TransactionScope();
            var createdDbConnection = new SqlConnection(createdDbConnectionString);
            createdDbConnection.Open();

            return createdDbConnection;
        }

        public override void DropDatabase(string databaseName)
            => this.transactionScope.Dispose();

        public override string GetDatabaseName() => databaseName;

        protected override string GetDataRecordFieldValue(IDataRecord dataRecord, int index)
        {
            if (!dataRecord.IsDBNull(index))
            {
                var fieldType = dataRecord.GetFieldType(index);

                if (fieldType == DateTimeType)
                {
                    return dataRecord.GetDateTime(index).ToString(DateTimeFormat, CultureInfo.InvariantCulture);
                }

                if (fieldType == DateTimeOffsetType)
                {
                    return ((SqlDataReader)dataRecord)
                        .GetDateTimeOffset(index)
                        .ToString(DateTimeOffsetFormat, CultureInfo.InvariantCulture);
                }

                if (fieldType == TimeSpanType)
                {
                    return ((SqlDataReader)dataRecord)
                        .GetTimeSpan(index)
                        .ToString(TimeSpanFormat, CultureInfo.InvariantCulture);
                }
            }

            return base.GetDataRecordFieldValue(dataRecord, index);
        }
    }
}
