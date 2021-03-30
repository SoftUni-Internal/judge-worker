namespace OJS.Workers.ExecutionStrategies.Sql.SqlServerSingleDatabase
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Globalization;
    using System.Text.RegularExpressions;
    using System.Transactions;

    public abstract class BaseSqlServerSingleDatabaseExecutionStrategy : BaseSqlExecutionStrategy
    {
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
        private const string DateTimeOffsetFormat = "yyyy-MM-dd HH:mm:ss.fffffff zzz";
        private const string TimeSpanFormat = "HH:mm:ss.fffffff";

        private static readonly Type DateTimeOffsetType = typeof(DateTimeOffset);

        private readonly string masterDbConnectionString;
        private readonly string restrictedUserId;
        private readonly string restrictedUserPassword;
        private readonly string databaseNameForSubmissionProcessor;

        private TransactionScope transactionScope;

        protected BaseSqlServerSingleDatabaseExecutionStrategy(
            string masterDbConnectionString,
            string restrictedUserId,
            string restrictedUserPassword,
            string submissionProcessorIdentifier)
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
            this.databaseNameForSubmissionProcessor = $"testing_{submissionProcessorIdentifier}";
        }

        public string WorkerDbConnectionString { get; set; }

        public string RestrictedUserId => $"{this.GetDatabaseName()}_{this.restrictedUserId}";

        public override IDbConnection GetOpenConnection(string databaseName)
        {
            this.EnsureDatabaseIsSetup();

            this.transactionScope = new TransactionScope();
            var createdDbConnection = new SqlConnection(this.WorkerDbConnectionString);
            createdDbConnection.Open();

            return createdDbConnection;
        }

        public override void DropDatabase(string databaseName)
            => this.transactionScope?.Dispose();

        public override string GetDatabaseName() => this.databaseNameForSubmissionProcessor;

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

        private void EnsureDatabaseIsSetup()
        {
            var databaseName = this.GetDatabaseName();

            using (var connection = new SqlConnection(this.masterDbConnectionString))
            {
                connection.Open();

                var setupDatabaseQuery =
                    $@"IF DB_ID('{databaseName}') IS NULL
                    BEGIN
                    CREATE DATABASE [{databaseName}];
                    CREATE LOGIN [{this.RestrictedUserId}] WITH PASSWORD=N'{this.restrictedUserPassword}',
                    DEFAULT_DATABASE=[master],
                    DEFAULT_LANGUAGE=[us_english],
                    CHECK_EXPIRATION=OFF,
                    CHECK_POLICY=ON;
                    END";

                var setupUserAsOwnerQuery = $@"
                    USE [{databaseName}];
                    IF IS_ROLEMEMBER('db_owner', '{this.RestrictedUserId}') = 0 OR IS_ROLEMEMBER('db_owner', '{this.RestrictedUserId}') is NULL
                    BEGIN
                    CREATE USER [{this.RestrictedUserId}] FOR LOGIN [{this.RestrictedUserId}];
                    ALTER ROLE [db_owner] ADD MEMBER [{this.RestrictedUserId}];
                    END";

                this.ExecuteNonQuery(connection, setupDatabaseQuery);
                this.ExecuteNonQuery(connection, setupUserAsOwnerQuery);
            }

            this.WorkerDbConnectionString = this.BuildWorkerDbConnectionString(databaseName);
        }

        private string BuildWorkerDbConnectionString(string databaseName)
        {
            var userIdRegex = new Regex("User Id=.*?;");
            var passwordRegex = new Regex("Password=.*?;");

            var workerDbConnectionString = this.masterDbConnectionString;

            workerDbConnectionString =
                userIdRegex.Replace(workerDbConnectionString, $"User Id={this.RestrictedUserId};");

            workerDbConnectionString =
                passwordRegex.Replace(workerDbConnectionString, $"Password={this.restrictedUserPassword}");

            workerDbConnectionString += $";Database={databaseName};Pooling=False;";

            return workerDbConnectionString;
        }
    }
}