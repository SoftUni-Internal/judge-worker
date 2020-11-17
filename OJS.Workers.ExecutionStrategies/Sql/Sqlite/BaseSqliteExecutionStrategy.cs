namespace OJS.Workers.ExecutionStrategies.Sql.Sqlite
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Globalization;
    using Microsoft.Data.Sqlite;

    public class BaseSqliteExecutionStrategy : BaseSqlExecutionStrategy
    {
        private const string ConnectionStringFormat = "Data Source={0};Mode=Memory;Cache=Shared";
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        private const string DateTimeOffsetFormat = "yyyy-MM-dd HH:mm:ss";
        private const string TimeSpanFormat = "HH:mm:ss";

        private static readonly Type DateTimeOffsetType = typeof(DateTimeOffset);

        public BaseSqliteExecutionStrategy()
        {
        }

        public override void DropDatabase(string databaseName)
        {
        }

        public override IDbConnection GetOpenConnection(string databaseName)
        {
            var connection = new SqliteConnection(GetConnectionString(databaseName));
            connection.Open();
            return connection;
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

        private static string GetConnectionString(string databaseName) => string.Format(ConnectionStringFormat, databaseName);
    }
}
