namespace OJS.Workers.ExecutionStrategies.SqlStrategies.SqlServerLocalDb
{
    using System.Data;

    using OJS.Workers.Common;
    using OJS.Workers.ExecutionStrategies.Models;

    public class SqlServerLocalDbPrepareDatabaseAndRunQueriesExecutionStrategy : BaseSqlServerLocalDbExecutionStrategy
    {
        public SqlServerLocalDbPrepareDatabaseAndRunQueriesExecutionStrategy(
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
            this.ExecuteNonQuery(connection, test.Input);
            var sqlTestResult = this.ExecuteReader(connection, executionContext.Code, executionContext.TimeLimit);
            this.ProcessSqlResult(sqlTestResult, executionContext, test, result);
        }
    }
}
