namespace OJS.Workers.ExecutionStrategies.Sql.MySql
{
    public class MySqlRunQueriesAndCheckDatabaseExecutionStrategyOriginal : MySqlRunQueriesAndCheckDatabaseExecutionStrategy
    {
        public MySqlRunQueriesAndCheckDatabaseExecutionStrategyOriginal(
            string sysDbConnectionString,
            string restrictedUserId,
            string restrictedUserPassword)
            : base(
                sysDbConnectionString,
                restrictedUserId,
                restrictedUserPassword)
        {
        }
    }
}