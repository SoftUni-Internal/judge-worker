namespace OJS.Workers.ExecutionStrategies.Sql.Postgres
{
    using System;
    using System.Data;
    using System.Text.RegularExpressions;
    using System.Transactions;
    using OJS.Workers.Common;
    using OJS.Workers.ExecutionStrategies.Models;
    using OJS.Workers.ExecutionStrategies.Sql.SqlServerLocalDb;
    using Npgsql;

    public abstract class BasePostgresExecutionStrategy : BaseSqlServerLocalDbExecutionStrategy
    {
        private readonly string databaseNameForSubmissionProcessor;

        private TransactionScope transactionScope;
        private IDbConnection currentConnection;

        protected BasePostgresExecutionStrategy(
            string masterDbConnectionString,
            string restrictedUserId,
            string restrictedUserPassword,
            string submissionProcessorIdentifier)
            : base(masterDbConnectionString, restrictedUserId, restrictedUserPassword)
            => this.databaseNameForSubmissionProcessor = $"worker_{submissionProcessorIdentifier}_do_not_delete";

        protected override string RestrictedUserId => $"{this.GetDatabaseName()}_{base.RestrictedUserId}";

        private string WorkerDbConnectionString { get; set; }


        public override IDbConnection GetOpenConnection(string databaseName)
        {
            if (this.currentConnection != null && (this.currentConnection.State & ConnectionState.Open) == 0)
            {
                this.currentConnection.Dispose();
                this.currentConnection = new NpgsqlConnection(this.WorkerDbConnectionString);
                this.currentConnection.Open();
                return this.currentConnection;
            }

            this.EnsureDatabaseIsSetup();

            // this.transactionScope = new TransactionScope();
            var connection = new NpgsqlConnection(this.WorkerDbConnectionString);
            connection.Open();
            this.currentConnection = connection;
            return connection;
        }

        public override void DropDatabase(string databaseName)
        {
            if (this.transactionScope != null)
            {
                this.transactionScope.Dispose();
            }
            else
            {
                var connection = this.GetOpenConnection(this.WorkerDbConnectionString);

                var dropDatabase = $@"
                    DROP DATABASE IF EXISTS {databaseName};
                ";

                this.ExecuteNonQuery(connection, dropDatabase);
            }
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

        protected override IExecutionResult<TestResult> Execute(IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result, Action<IDbConnection, TestContext> executionFlow)
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
            var dropAllTables = @"
                DO $$ DECLARE
    r RECORD;
                BEGIN
                -- if the schema you operate on is not ""current"", you will want to
                            -- replace current_schema() in query with 'schematodeletetablesfrom'
                            -- *and* update the generate 'DROP...' accordingly.
                            FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = current_schema()) LOOP
                            EXECUTE 'DROP TABLE IF EXISTS ' || quote_ident(r.tablename) || ' CASCADE';
                        END LOOP;
                        END $$;
            ";

            // var grantPermissions = @"
            //     GRANT ALL ON SCHEMA public TO postgres;
            //     GRANT ALL ON SCHEMA public TO public;
            // ";

            this.ExecuteNonQuery(connection, dropAllTables);
            // this.ExecuteNonQuery(connection, grantPermissions);
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
                catch (Exception _)
                {
                    // PG doesn't support IF Exists in one transaction
                    this.CleanUpDb(connection);
                }
                finally
                {
                    connection.Dispose();
                }
            }

            this.WorkerDbConnectionString = this.BuildWorkerDbConnectionString(databaseName);
        }
    }
}