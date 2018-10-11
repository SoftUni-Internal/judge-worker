namespace OJS.Workers.ExecutionStrategies.SqlStrategies
{
    using System;
    using System.Data;
    using System.Globalization;

    using MissingFeatures;

    using OJS.Workers.Checkers;
    using OJS.Workers.Common;
    using OJS.Workers.Common.Extensions;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.Common.Models;
    using OJS.Workers.ExecutionStrategies.Models;

    public abstract class BaseSqlExecutionStrategy : IExecutionStrategy
    {
        protected static readonly Type DecimalType = typeof(decimal);
        protected static readonly Type DoubleType = typeof(double);
        protected static readonly Type FloatType = typeof(float);
        protected static readonly Type ByteArrayType = typeof(byte[]);
        protected static readonly Type DateTimeType = typeof(DateTime);
        protected static readonly Type TimeSpanType = typeof(TimeSpan);

        private const int DefaultTimeLimit = 2 * 60 * 1000;

        public string WorkingDirectory { get; set; } 

        public IExecutionResult<TResult> SafeExecute<TResult>(IExecutionContext executionContext)
            where TResult : ISingleCodeRunResult, new()
        {
            this.WorkingDirectory = DirectoryHelpers.CreateTempDirectoryForExecutionStrategy();
            try
            {
                return this.Execute<TResult>(executionContext);
            }
            finally
            {
                DirectoryHelpers.SafeDeleteDirectory(this.WorkingDirectory, true);
            }
        }

        public virtual IExecutionResult<TResult> Execute<TResult>(IExecutionContext executionContext)
            where TResult : ISingleCodeRunResult, new()
        {
            switch (executionContext)
            {
                case CompetitiveExecutionContext competitiveExecutionContext:
                    return (IExecutionResult<TResult>)this.ExecuteCompetitive(competitiveExecutionContext);
                default:
                    return new ExecutionResult<TResult>
                    {
                        IsCompiledSuccessfully = false,
                        CompilerComment = "Execution context not found"
                    };
            }
        }

        public virtual IExecutionResult<TestResult> Execute(
            CompetitiveExecutionContext executionContext,
            Action<IDbConnection, TestContext, ExecutionResult<TestResult>> executionFlow)
        {
            var result = new ExecutionResult<TestResult> { IsCompiledSuccessfully = true };

            string databaseName = null;
            try
            {
                foreach (var test in executionContext.Tests)
                {
                    databaseName = this.GetDatabaseName();

                    using (var connection = this.GetOpenConnection(databaseName))
                    {
                        executionFlow(connection, test, result);
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

            return result;
        }

        public abstract IDbConnection GetOpenConnection(string databaseName);

        public abstract void DropDatabase(string databaseName);

        public virtual string GetDatabaseName() => Guid.NewGuid().ToString();

        protected abstract IExecutionResult<TestResult> ExecuteCompetitive(
            CompetitiveExecutionContext competitiveExecutionContext);

        protected virtual string GetDataRecordFieldValue(IDataRecord dataRecord, int index)
        {
            string result;

            if (dataRecord.IsDBNull(index))
            {
                result = null;
            }
            else
            {
                var fieldType = dataRecord.GetFieldType(index);

                // Using CultureInfo.InvariantCulture to have consistent decimal separator.
                if (fieldType == DecimalType)
                {
                    result = dataRecord.GetDecimal(index).ToString(CultureInfo.InvariantCulture);
                }
                else if (fieldType == DoubleType)
                {
                    result = dataRecord.GetDouble(index).ToString(CultureInfo.InvariantCulture);
                }
                else if (fieldType == FloatType)
                {
                    result = dataRecord.GetFloat(index).ToString(CultureInfo.InvariantCulture);
                }
                else if (fieldType == ByteArrayType)
                {
                    var bytes = (byte[])dataRecord.GetValue(index);
                    result = bytes.ToHexString();
                }
                else
                {
                    result = dataRecord.GetValue(index).ToString();
                }
            }

            return result;
        }

        protected bool ExecuteNonQuery(IDbConnection connection, string commandText, int timeLimit = DefaultTimeLimit)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;

                return Code.ExecuteWithTimeLimit(
                    TimeSpan.FromMilliseconds(timeLimit),
                    () => command.ExecuteNonQuery());
            }
        }

        protected SqlResult ExecuteReader(IDbConnection connection, string commandText, int timeLimit = DefaultTimeLimit)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;

                using (var reader = command.ExecuteReader())
                {
                    var sqlTestResult = new SqlResult();
                    sqlTestResult.Completed = Code.ExecuteWithTimeLimit(
                        TimeSpan.FromMilliseconds(timeLimit),
                        () =>
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
                        });

                    return sqlTestResult;
                }
            }
        }

        protected void ProcessSqlResult(
            SqlResult sqlResult,
            CompetitiveExecutionContext executionContext,
            TestContext test,
            IExecutionResult<TestResult> result)
        {
            if (sqlResult.Completed)
            {
                var joinedUserOutput = string.Join(Environment.NewLine, sqlResult.Results);

                var checker = Checker.CreateChecker(executionContext.CheckerAssemblyName, executionContext.CheckerTypeName, executionContext.CheckerParameter);
                var checkerResult = checker.Check(test.Input, joinedUserOutput, test.Output, test.IsTrialTest);

                result.Results.Add(new TestResult
                {
                    Id = test.Id,
                    ResultType = checkerResult.IsCorrect ? TestRunResultType.CorrectAnswer : TestRunResultType.WrongAnswer,
                    CheckerDetails = checkerResult.CheckerDetails
                });
            }
            else
            {
                result.Results.Add(new TestResult
                {
                    Id = test.Id,
                    TimeUsed = executionContext.TimeLimit,
                    ResultType = TestRunResultType.TimeLimit
                });
            }
        }
    }
}
