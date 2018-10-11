namespace OJS.Workers.ExecutionStrategies.SqlStrategies.SqlServerLocalDb
{
    using OJS.Workers.ExecutionStrategies.Models;

    public class SqlServerLocalDbRunSkeletonRunQueriesAndCheckDatabaseExecutionStrategy : BaseSqlServerLocalDbExecutionStrategy
    {
        public SqlServerLocalDbRunSkeletonRunQueriesAndCheckDatabaseExecutionStrategy(
            string masterDbConnectionString,
            string restrictedUserId,
            string restrictedUserPassword)
            : base(masterDbConnectionString, restrictedUserId, restrictedUserPassword)
        {
        }

        protected override ExecutionResult ExecuteCompetitive(CompetitiveExecutionContext executionContext)
        {
            return this.Execute(
                executionContext,
                (connection, test, result) =>
                {
                    this.ExecuteNonQuery(connection, executionContext.TaskSkeletonAsString);
                    this.ExecuteNonQuery(connection, executionContext.Code, executionContext.TimeLimit);
                    var sqlTestResult = this.ExecuteReader(connection, test.Input);
                    this.ProcessSqlResult(sqlTestResult, executionContext, test, result);
                });
        }
    }
}
