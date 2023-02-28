namespace OJS.Workers.ExecutionStrategies.Sql.SqlServerLocalDb
{
    using System;
    using System.Data;
    using System.Data.SqlClient;

    public abstract class BaseSqlServerLocalDbExecutionStrategy : BaseSqlServerAndPostgreSqlExecutionStrategy
    {
        protected BaseSqlServerLocalDbExecutionStrategy(
            string masterDbConnectionString,
            string restrictedUserId,
            string restrictedUserPassword)
            : base(masterDbConnectionString, restrictedUserId, restrictedUserPassword)
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

            this.MasterDbConnectionString = masterDbConnectionString;
            this.RestrictedUserId = restrictedUserId;
            this.RestrictedUserPassword = restrictedUserPassword;
        }

        protected string MasterDbConnectionString { get; }

        protected virtual string RestrictedUserId { get; }

        protected string RestrictedUserPassword { get; }

        public override IDbConnection GetOpenConnection(string databaseName)
        {
            var databaseFilePath = $"{this.WorkingDirectory}\\{databaseName}.mdf";

            using (var connection = new SqlConnection(this.MasterDbConnectionString))
            {
                connection.Open();

                var createDatabaseQuery =
                    $"CREATE DATABASE [{databaseName}] ON PRIMARY (NAME=N'{databaseName}', FILENAME=N'{databaseFilePath}');";

                var createLoginQuery = $@"
                    IF NOT EXISTS (SELECT name FROM master.sys.server_principals WHERE name=N'{this.RestrictedUserId}')
                    BEGIN
                    CREATE LOGIN [{this.RestrictedUserId}] WITH PASSWORD=N'{this.RestrictedUserPassword}',
                    DEFAULT_DATABASE=[master],
                    DEFAULT_LANGUAGE=[us_english],
                    CHECK_EXPIRATION=OFF,
                    CHECK_POLICY=ON;
                    END;";

                var createUserAsDbOwnerQuery = $@"
                    USE [{databaseName}];
                    CREATE USER [{this.RestrictedUserId}] FOR LOGIN [{this.RestrictedUserId}];
                    ALTER ROLE [db_owner] ADD MEMBER [{this.RestrictedUserId}];";

                this.ExecuteNonQuery(connection, createDatabaseQuery);

                this.ExecuteNonQuery(connection, createLoginQuery);

                this.ExecuteNonQuery(connection, createUserAsDbOwnerQuery);
            }

            var createdDbConnectionString = this.BuildWorkerDbConnectionString(databaseName);

            var createdDbConnection = new SqlConnection(createdDbConnectionString);
            createdDbConnection.Open();

            return createdDbConnection;
        }
    }
}
