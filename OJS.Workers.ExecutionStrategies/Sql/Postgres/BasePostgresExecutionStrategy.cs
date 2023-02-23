namespace OJS.Workers.ExecutionStrategies.Sql.Postgres
{
    using System;
    using System.Data;
    using System.Text.RegularExpressions;
    using OJS.Workers.Common;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.ExecutionStrategies.Sql.SqlServerLocalDb;
    using Npgsql;

    public abstract class BasePostgresExecutionStrategy : BaseSqlServerLocalDbExecutionStrategy
    {
        private readonly string databaseNameForSubmissionProcessor;
        private string workerDbConnectionString;
        private IDbConnection currentConnection;

        protected BasePostgresExecutionStrategy(
            string masterDbConnectionString,
            string restrictedUserId,
            string restrictedUserPassword,
            string submissionProcessorIdentifier)
            : base(masterDbConnectionString, restrictedUserId, restrictedUserPassword)
            => this.databaseNameForSubmissionProcessor = $"worker_{submissionProcessorIdentifier}_do_not_delete";

        protected override string RestrictedUserId => $"{this.GetDatabaseName()}_{base.RestrictedUserId}";

        public override IDbConnection GetOpenConnection(string databaseName)
        {
            if (this.currentConnection != null && (this.currentConnection.State & ConnectionState.Open) == 0)
            {
                this.currentConnection.Dispose();
                this.currentConnection = new NpgsqlConnection(this.workerDbConnectionString);
                this.currentConnection.Open();
                return this.currentConnection;
            }

            this.EnsureDatabaseIsSetup();

            var connection = new NpgsqlConnection(this.workerDbConnectionString);
            connection.Open();
            this.currentConnection = connection;
            return connection;
        }

        public override string GetDatabaseName() => this.databaseNameForSubmissionProcessor;

        protected override string BuildWorkerDbConnectionString(string databaseName)
        {
            var userIdRegex = new Regex("UserId=.*?;");
            var passwordRegex = new Regex("Password=.*?;");

            var createdDbConnectionString = this.MasterDbConnectionString;

            createdDbConnectionString =
                userIdRegex.Replace(createdDbConnectionString, $"User Id={this.RestrictedUserId};");

            createdDbConnectionString =
                passwordRegex.Replace(createdDbConnectionString, $"Password={this.RestrictedUserPassword}");

            createdDbConnectionString += $";Database={databaseName};";

            return createdDbConnectionString;
        }

        protected override IExecutionResult<TestResult> Execute(IExecutionContext<TestsInputModel> executionContext, IExecutionResult<TestResult> result, Action<IDbConnection, TestContext> executionFlow)
        {
            result.IsCompiledSuccessfully = true;

            try
            {
                foreach (var test in executionContext.Input.Tests)
                {
                    using (var connection = this.GetOpenConnection(this.GetDatabaseName()))
                    {
                        executionFlow(connection, test);
                        this.CleanUpDb(connection);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(this.GetDatabaseName()))
                {
                    using (var connection = this.GetOpenConnection(this.GetDatabaseName()))
                    {
                        this.CleanUpDb(connection);
                    }
                }

                result.IsCompiledSuccessfully = false;
                result.CompilerComment = ex.Message;
            }

            return result;
        }

        private void CleanUpDb(IDbConnection connection)
        {
            var dropPublicScheme = @"
                DROP SCHEMA public CASCADE;
                CREATE SCHEMA public;
            ";

            var grantPermissions = @"
                GRANT ALL ON SCHEMA public TO postgres;
                GRANT ALL ON SCHEMA public TO public;
            ";

            this.ExecuteNonQuery(connection, dropPublicScheme);
            this.ExecuteNonQuery(connection, grantPermissions);
        }

        private void EnsureDatabaseIsSetup()
        {
            var databaseName = this.GetDatabaseName();
            var connectionString = this.MasterDbConnectionString;

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                var createUserQuery = $@"
                    DO
                    $$
                    BEGIN
                      IF NOT EXISTS (SELECT * FROM pg_user WHERE usename = '{this.RestrictedUserId}') THEN
                        CREATE USER {this.RestrictedUserId} WITH PASSWORD '{this.RestrictedUserPassword}';
                      end if;
                    end
                    $$
                    ;
                ";

                this.ExecuteNonQuery(connection, createUserQuery);
                try
                {
                    var setupDatabaseQuery = $@"
                        CREATE DATABASE {databaseName} OWNER {this.RestrictedUserId}
                    ";

                    this.ExecuteNonQuery(connection, setupDatabaseQuery);
                }
                catch (Exception)
                {
                    // PG doesn't support CREATE DATABASE IF Exists
                    this.CleanUpDb(connection);
                }
            }

            this.workerDbConnectionString = this.BuildWorkerDbConnectionString(databaseName);
        }
    }
}