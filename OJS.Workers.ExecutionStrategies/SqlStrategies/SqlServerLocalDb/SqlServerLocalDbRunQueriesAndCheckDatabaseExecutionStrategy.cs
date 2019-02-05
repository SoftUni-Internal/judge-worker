namespace OJS.Workers.ExecutionStrategies.SqlStrategies.SqlServerLocalDb
{
    using OJS.Workers.Common;
    using OJS.Workers.ExecutionStrategies.Models;

    public class SqlServerLocalDbRunQueriesAndCheckDatabaseExecutionStrategy : BaseSqlServerLocalDbExecutionStrategy
    {
        public SqlServerLocalDbRunQueriesAndCheckDatabaseExecutionStrategy(
            string masterDbConnectionString,
            string restrictedUserId,
            string restrictedUserPassword)
            : base(masterDbConnectionString, restrictedUserId, restrictedUserPassword)
        {
        }

        protected override void ExecuteAgainstTestsInput(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result)
            => this.ExecuteAgainstTests(
                executionContext,
                result,
                (connection, test) =>
                {
                    this.ExecuteNonQuery(connection, executionContext.Code, executionContext.TimeLimit);
                    var sqlTestResult = this.ExecuteReader(connection, test.Input);
                    this.ProcessSqlResult(sqlTestResult, executionContext, test, result);
                });
    }
}
