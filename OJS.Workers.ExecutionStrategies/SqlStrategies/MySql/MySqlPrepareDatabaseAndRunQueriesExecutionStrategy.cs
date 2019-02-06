namespace OJS.Workers.ExecutionStrategies.SqlStrategies.MySql
{
    using System.Data;

    using OJS.Workers.Common;
    using OJS.Workers.ExecutionStrategies.Models;

    public class MySqlPrepareDatabaseAndRunQueriesExecutionStrategy : BaseMySqlExecutionStrategy
    {
        public MySqlPrepareDatabaseAndRunQueriesExecutionStrategy(
            string sysDbConnectionString,
            string restrictedUserId,
            string restrictedUserPassword)
            : base(sysDbConnectionString, restrictedUserId, restrictedUserPassword)
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
