﻿namespace OJS.Workers.ExecutionStrategies.Sql.PostgreSql
{
    using System;
    using System.Data;
    using System.Globalization;
    using System.Text.RegularExpressions;
    using Npgsql;
    using OJS.Workers.Common;
    using OJS.Workers.ExecutionStrategies.Models;

    public abstract class BasePostgreSqlExecutionStrategy : BaseSqlExecutionStrategy
    {
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        private const string TimeSpanFormat = @"hh\:mm\:ss";

        private readonly string databaseNameForSubmissionProcessor;
        private string workerDbConnectionString;
        private IDbConnection currentConnection;
        private bool isDisposed;

        protected BasePostgreSqlExecutionStrategy(
            string masterDbConnectionString,
            string restrictedUserId,
            string restrictedUserPassword,
            string submissionProcessorIdentifier)
            : base(masterDbConnectionString, restrictedUserId, restrictedUserPassword)
            => this.databaseNameForSubmissionProcessor = $"worker_{submissionProcessorIdentifier}_do_not_delete";

        protected override string RestrictedUserId => $"{this.GetDatabaseName()}_{base.RestrictedUserId}";

        public override IDbConnection GetOpenConnection(string databaseName)
        {
            if (this.currentConnection != null)
            {
                return this.CreateConnection();
            }

            this.EnsureDatabaseIsSetup();

            if (this.currentConnection != null && this.isDisposed)
            {
                this.currentConnection.Dispose();
            }

            return this.CreateConnection();
        }

        public override string GetDatabaseName() => this.databaseNameForSubmissionProcessor;

        public override void DropDatabase(string databaseName)
        {
        }

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
                this.ExecuteBeforeTests(this.GetOpenConnection(this.GetDatabaseName()), executionContext);

                foreach (var test in executionContext.Input.Tests)
                {
                    using (var connection = this.GetOpenConnection(this.GetDatabaseName()))
                    {
                        this.ExecuteBeforeEachTest(connection, executionContext, test);
                        executionFlow(connection, test);
                        this.ExecuteAfterEachTest(connection, executionContext, test);
                    }
                }

                this.ExecuteAfterTests(this.GetOpenConnection(this.GetDatabaseName()), executionContext);
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

        protected virtual void ExecuteBeforeTests(IDbConnection connection, IExecutionContext<TestsInputModel>
            executionContext)
        {
        }

        protected virtual void ExecuteAfterTests(IDbConnection connection, IExecutionContext<TestsInputModel>
            executionContext) => this.CleanUpDb(connection);

        protected virtual void ExecuteBeforeEachTest(IDbConnection connection, IExecutionContext<TestsInputModel> executionContext, TestContext test)
        {
        }

        protected virtual void ExecuteAfterEachTest(IDbConnection connection, IExecutionContext<TestsInputModel> executionContext, TestContext test)
        {
        }

        protected override bool ExecuteNonQuery(IDbConnection connection, string commandText, int timeLimit =
            DefaultTimeLimit)
        {
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = timeLimit / 1000;
                    command.CommandText = this.FixCommandText(commandText);

                    command.ExecuteNonQuery();
                }
            }
            catch (TimeoutException)
            {
                return false;
            }

            return true;
        }

        protected override SqlResult ExecuteReader(
            IDbConnection connection,
            string commandText,
            int timeLimit = DefaultTimeLimit)
        {
            var sqlTestResult = new SqlResult { Completed = true };

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = commandText;
                    command.CommandTimeout = timeLimit / 1000;

                    using (var reader = command.ExecuteReader())
                    {
                        do
                        {
                            while (reader.Read())
                            {
                                for (var i = 0; i < reader.FieldCount; i++)
                                {
                                    var fieldValue = this.GetDataRecordFieldValue(reader, i);

                                    sqlTestResult.Results.Add(fieldValue);
                                }
                            }
                        }
                        while (reader.NextResult());
                    }
                }
            }
            catch (TimeoutException)
            {
                sqlTestResult.Completed = false;
            }

            return sqlTestResult;
        }

        protected override string GetDataRecordFieldValue(IDataRecord dataRecord, int index)
        {
            if (dataRecord.IsDBNull(index))
            {
                return base.GetDataRecordFieldValue(dataRecord, index);
            }

            var fieldType = dataRecord.GetFieldType(index);

            if (fieldType == DateTimeType)
            {
                return dataRecord.GetDateTime(index)
                    .ToString(DateTimeFormat, CultureInfo.InvariantCulture);
            }

            if (fieldType == TimeSpanType)
            {
                return ((NpgsqlDataReader)dataRecord)
                    .GetTimeSpan(index)
                    .ToString(TimeSpanFormat, CultureInfo.InvariantCulture);
            }

            return base.GetDataRecordFieldValue(dataRecord, index);
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

        private IDbConnection CreateConnection()
        {
            var connection = new NpgsqlConnection(this.workerDbConnectionString);
            connection.Open();
            connection.Disposed += (sender, args) =>
            {
                this.isDisposed = true;
            };

            this.currentConnection = connection;
            this.isDisposed = false;
            return this.currentConnection;
        }
    }
}