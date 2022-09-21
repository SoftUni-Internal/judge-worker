#pragma warning disable SA1200
using MySql.Data.MySqlClient;
#pragma warning disable SA1208
using System.Collections.Generic;
#pragma warning restore SA1208
using System.Threading.Tasks;
#pragma warning disable SA1208
using System.Diagnostics.CodeAnalysis;
#pragma warning restore SA1208

#pragma warning restore SA1200

namespace OJS.Workers.ExecutionStrategies.Sql.MySql
{
    using System;
    using System.Data;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using OJS.Workers.Common;
    using OJS.Workers.ExecutionStrategies.Models;

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access")]
    public class MySqlRunQueriesAndCheckDatabaseExecutionStrategyCleanUpDatabase : BaseSqlExecutionStrategy
    {
        private readonly string sysDbConnectionString;
        private readonly string restrictedUserId;
        private readonly string restrictedUserPassword;

        public MySqlRunQueriesAndCheckDatabaseExecutionStrategyCleanUpDatabase(
            string sysDbConnectionString,
            string restrictedUserId,
            string restrictedUserPassword)
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

                    this.DropDatabase(databaseName);
                }
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(databaseName))
                {
                    this.DropDatabase(databaseName);
                }

                result.IsCompiledSuccessfully = false;
                result.CompilerComment = ex.Message;
            }

            st.Stop();
            result.StopWatchResult = st.Elapsed.ToString();

            return result;
        }

        public override IDbConnection GetOpenConnection(string databaseName)
        {
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

                this.ExecuteNonQuery(connection, createDatabaseQuery);
                this.ExecuteNonQuery(connection, createUserQuery);
                this.ExecuteNonQuery(connection, grandPrivilegesToUserQuery);
                this.ExecuteNonQuery(connection, enableLogBinTrustFunctionCreatorsQuery);
            }

            var workerConnection = new MySqlConnection(this.BuildWorkerDbConnectionString(databaseName));
            workerConnection.Open();

            return workerConnection;
        }

        public override void DropDatabase(string databaseName)
        {
            using (var connection = new MySqlConnection(this.sysDbConnectionString))
            {
                connection.Open();

                this.ExecuteNonQuery(connection, $"DROP DATABASE IF EXISTS `{databaseName}`;");
            }
        }

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