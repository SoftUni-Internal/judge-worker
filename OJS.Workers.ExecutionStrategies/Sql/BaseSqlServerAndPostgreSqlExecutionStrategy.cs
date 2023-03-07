namespace OJS.Workers.ExecutionStrategies.Sql
{
    using System;
    using System.Text.RegularExpressions;
    using System.Data;
    using System.Data.SqlClient;
    using System.Globalization;

    public abstract class BaseSqlServerAndPostgreSqlExecutionStrategy : BaseSqlExecutionStrategy
    {
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
        private const string DateTimeOffsetFormat = "yyyy-MM-dd HH:mm:ss.fffffff zzz";
        private const string TimeSpanFormat = "HH:mm:ss.fffffff";

        private static readonly Type DateTimeOffsetType = typeof(DateTimeOffset);

        protected BaseSqlServerAndPostgreSqlExecutionStrategy(
            string masterDbConnectionString,
            string restrictedUserId,
            string restrictedUserPassword)
            : base(masterDbConnectionString, restrictedUserId, restrictedUserPassword)
        {
        }

        public override void DropDatabase(string databaseName)
        {
            using (var connection = new SqlConnection(this.MasterDbConnectionString))
            {
                connection.Open();

                var dropDatabaseQuery = $@"
                    IF EXISTS (SELECT name FROM master.sys.databases WHERE name=N'{databaseName}')
                    BEGIN
                    ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{databaseName}];
                    END;";

                this.ExecuteNonQuery(connection, dropDatabaseQuery);
            }
        }

        protected override string BuildWorkerDbConnectionString(string databaseName)
        {
            var userIdRegex = new Regex("User Id=.*?;");
            var passwordRegex = new Regex("Password=.*?;");

            var createdDbConnectionString = this.MasterDbConnectionString;

            createdDbConnectionString =
                userIdRegex.Replace(createdDbConnectionString, $"User Id={this.RestrictedUserId};");

            createdDbConnectionString =
                passwordRegex.Replace(createdDbConnectionString, $"Password={this.RestrictedUserPassword}");

            createdDbConnectionString += $";Database={databaseName};Pooling=False;";

            return createdDbConnectionString;
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