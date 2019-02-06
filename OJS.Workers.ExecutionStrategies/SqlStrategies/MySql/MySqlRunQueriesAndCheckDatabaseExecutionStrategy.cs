namespace OJS.Workers.ExecutionStrategies.SqlStrategies.MySql
{
    using OJS.Workers.Common;
    using OJS.Workers.ExecutionStrategies.Models;

    public class MySqlRunQueriesAndCheckDatabaseExecutionStrategy : BaseMySqlExecutionStrategy
    {
        public MySqlRunQueriesAndCheckDatabaseExecutionStrategy(
            string sysDbConnectionString,
            string restrictedUserId,
            string restrictedUserPassword)
            : base(sysDbConnectionString, restrictedUserId, restrictedUserPassword)
        {
        }

        protected override void ExecuteAgainstTestsInput(
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
    }
}
