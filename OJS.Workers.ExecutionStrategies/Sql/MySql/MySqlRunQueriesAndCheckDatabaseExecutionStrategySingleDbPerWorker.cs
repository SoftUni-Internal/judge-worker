#pragma warning disable SA1200
using MySql.Data.MySqlClient;

#pragma warning restore SA1200

namespace OJS.Workers.ExecutionStrategies.Sql.MySql
{
    using System;
    using System.Data;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using OJS.Workers.Common;
    using OJS.Workers.ExecutionStrategies.Models;

    public class MySqlRunQueriesAndCheckDatabaseExecutionStrategySingleDbPerWorker : BaseSqlExecutionStrategy
    {
        private readonly string sysDbConnectionString;
        private readonly string restrictedUserId;
        private readonly string restrictedUserPassword;
#pragma warning disable SA1309
#pragma warning disable SA1204
        private static MySqlConnection _connection;
#pragma warning restore SA1204
#pragma warning restore SA1309

        public MySqlRunQueriesAndCheckDatabaseExecutionStrategySingleDbPerWorker(
            string sysDbConnectionString,
            string restrictedUserId,
            string restrictedUserPassword)
            : base()
        {
            this.sysDbConnectionString = sysDbConnectionString;
            this.restrictedUserId = restrictedUserId;
            this.restrictedUserPassword = restrictedUserPassword;
        }

        protected override IExecutionResult<TestResult> ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
            => this.Execute(
                executionContext,
                result,
                (connection, test) =>
                {
                    this.ExecuteNonQuery(connection, executionContext.Code, executionContext.TimeLimit);
                    var sqlTestResult = this.ExecuteReader(connection, test.Input);
                    this.ProcessSqlResult(sqlTestResult, executionContext, test, result);
                });

        protected override IExecutionResult<TestResult> Execute(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result,
            Action<IDbConnection, TestContext> executionFlow)
        {
            result.IsCompiledSuccessfully = true;

            var st = Stopwatch.StartNew();

            string databaseName = null;
            try
            {
                databaseName = this.GetDatabaseName();
                using (var connection = this.GetOpenConnection(databaseName))
                {
                    foreach (var test in executionContext.Input.Tests)
                    {
                        executionFlow(connection, test);

                        var cleanUpAfterTest = $@"
                        START TRANSACTION ;
                        SET FOREIGN_KEY_CHECKS = 0;
                        SET GROUP_CONCAT_MAX_LEN=32768;
                        SET @tables = NULL;
                        SELECT GROUP_CONCAT('`', table_name, '`') INTO @tables
                        FROM information_schema.tables
                        WHERE table_schema = '{databaseName}';
                        SELECT IFNULL(@tables,'dummy') INTO @tables;
                        SET @tables = CONCAT('DROP TABLE IF EXISTS ', @tables);
                        PREPARE stmt FROM @tables;
                        EXECUTE stmt;
                        DEALLOCATE PREPARE stmt;
                        SET FOREIGN_KEY_CHECKS = 1;
                        COMMIT ;";

                        this.ExecuteNonQuery(connection, cleanUpAfterTest);
                    }
                }
            }
            catch (Exception ex)
            {
                result.IsCompiledSuccessfully = false;
                result.CompilerComment = ex.Message;
                using (var connection = this.GetOpenConnection(databaseName))
                {
                    var cleanUpAfterTest = $@"
                        START TRANSACTION ;
                        SET FOREIGN_KEY_CHECKS = 0;
                        SET GROUP_CONCAT_MAX_LEN=32768;
                        SET @tables = NULL;
                        SELECT GROUP_CONCAT('`', table_name, '`') INTO @tables
                        FROM information_schema.tables
                        WHERE table_schema = '{databaseName}';
                        SELECT IFNULL(@tables,'dummy') INTO @tables;
                        SET @tables = CONCAT('DROP TABLE IF EXISTS ', @tables);
                        PREPARE stmt FROM @tables;
                        EXECUTE stmt;
                        DEALLOCATE PREPARE stmt;
                        SET FOREIGN_KEY_CHECKS = 1;
                        COMMIT ;";

                    this.ExecuteNonQuery(connection, cleanUpAfterTest);
                }
            }

            result.StopWatchResult = st.Elapsed.ToString();
            st.Stop();

            return result;
        }

#pragma warning disable SA1202
        public override IDbConnection GetOpenConnection(string databaseName)
#pragma warning restore SA1202
        {
            if (_connection != null)
            {
                try
                {
                    if (_connection.State != ConnectionState.Open)
                    {
                        _connection.Open();
                    }
                }
                catch (Exception)
                {
                    return _connection;
                }

                return _connection;
            }

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

                var enableLogBinTrustFunctionCreatorsQuery = "SET GLOBAL log_bin_trust_function_creators = 1;";

                try
                {
                    this.ExecuteNonQuery(connection, createDatabaseQuery);
                }
                catch (MySqlException)
                {
                    var currentWorkerConnection = new MySqlConnection(this.BuildWorkerDbConnectionString(databaseName));
                    currentWorkerConnection.Open();
                    _connection = currentWorkerConnection;

                    return currentWorkerConnection;
                }

                this.ExecuteNonQuery(connection, createUserQuery);
                this.ExecuteNonQuery(connection, grandPrivilegesToUserQuery);
                this.ExecuteNonQuery(connection, enableLogBinTrustFunctionCreatorsQuery);
            }

            var workerConnection = new MySqlConnection(this.BuildWorkerDbConnectionString(databaseName));
            workerConnection.Open();
            _connection = workerConnection;

            return workerConnection;
        }

        public override void DropDatabase(string databaseName) => throw new NotImplementedException();

        public override string GetDatabaseName() => "TestDb";

        private string BuildWorkerDbConnectionString(string databaseName)
        {
            var userIdRegex = new Regex("UID=.*?;");
            var passwordRegex = new Regex("Password=.*?;");

            var workerDbConnectionString = this.sysDbConnectionString;

            workerDbConnectionString =
                userIdRegex.Replace(workerDbConnectionString, $"UID={this.restrictedUserId};");

            workerDbConnectionString =
                passwordRegex.Replace(workerDbConnectionString, $"Password={this.restrictedUserPassword}");

            workerDbConnectionString += $";Database={databaseName};Pooling=False;Allow User Variables=True;";

            return workerDbConnectionString;
        }
    }
}