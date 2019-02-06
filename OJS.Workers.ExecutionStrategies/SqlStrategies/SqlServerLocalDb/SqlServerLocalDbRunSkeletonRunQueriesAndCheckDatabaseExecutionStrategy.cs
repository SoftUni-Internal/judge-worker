namespace OJS.Workers.ExecutionStrategies.SqlStrategies.SqlServerLocalDb
{
    using System.Data;

    using OJS.Workers.Common;
    using OJS.Workers.ExecutionStrategies.Models;

    public class SqlServerLocalDbRunSkeletonRunQueriesAndCheckDatabaseExecutionStrategy
        : BaseSqlServerLocalDbExecutionStrategy
    {
        public SqlServerLocalDbRunSkeletonRunQueriesAndCheckDatabaseExecutionStrategy(
            string masterDbConnectionString,
            string restrictedUserId,
            string restrictedUserPassword)
            : base(masterDbConnectionString, restrictedUserId, restrictedUserPassword)
        {
        }

        protected override void ExecuteAgainstTest(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result,
            IDbConnection connection,
            TestContext test)
        {
            this.ExecuteNonQuery(connection, executionContext.Input.TaskSkeletonAsString);
            this.ExecuteNonQuery(connection, executionContext.Code, executionContext.TimeLimit);
            var sqlTestResult = this.ExecuteReader(connection, test.Input);
            this.ProcessSqlResult(sqlTestResult, executionContext, test, result);
        }
    }
}
