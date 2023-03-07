﻿namespace OJS.Workers.ExecutionStrategies.Sql
{
    using System;
    using System.Data;
    using System.Globalization;
    using OJS.Workers.Common;
    using OJS.Workers.Common.Extensions;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.Common.Models;
    using OJS.Workers.ExecutionStrategies.Models;

    public abstract class BaseSqlExecutionStrategy : BaseExecutionStrategy
    {
        protected const int DefaultTimeLimit = 2 * 60 * 1000;
        protected static readonly Type DecimalType = typeof(decimal);
        protected static readonly Type DoubleType = typeof(double);
        protected static readonly Type FloatType = typeof(float);
        protected static readonly Type ByteArrayType = typeof(byte[]);
        protected static readonly Type DateTimeType = typeof(DateTime);
        protected static readonly Type TimeSpanType = typeof(TimeSpan);

        protected BaseSqlExecutionStrategy(
            string masterDbConnectionString,
            string restrictedUserId,
            string restrictedUserPassword)
        {
            if (string.IsNullOrWhiteSpace(masterDbConnectionString))
            {
                throw new ArgumentException("Invalid master DB connection string!", nameof(masterDbConnectionString));
            }

            if (string.IsNullOrWhiteSpace(restrictedUserId))
            {
                throw new ArgumentException("Invalid restricted user ID!", nameof(restrictedUserId));
            }

            if (string.IsNullOrWhiteSpace(restrictedUserPassword))
            {
                throw new ArgumentException("Invalid restricted user password!", nameof(restrictedUserPassword));
            }

            this.MasterDbConnectionString = masterDbConnectionString;
            this.RestrictedUserId = restrictedUserId;
            this.RestrictedUserPassword = restrictedUserPassword;
        }

        protected string MasterDbConnectionString { get; }

        protected virtual string RestrictedUserId { get; }

        protected string RestrictedUserPassword { get; }

        public abstract IDbConnection GetOpenConnection(string databaseName);

        public abstract void DropDatabase(string databaseName);

        public virtual string GetDatabaseName() => Guid.NewGuid().ToString();

        protected abstract string BuildWorkerDbConnectionString(string databaseName);

        protected virtual IExecutionResult<TestResult> Execute(
            IExecutionContext<TestsInputModel> executionContext,
            IExecutionResult<TestResult> result,
            Action<IDbConnection, TestContext> executionFlow)
        {
            result.IsCompiledSuccessfully = true;

            string databaseName = null;
            try
            {
                foreach (var test in executionContext.Input.Tests)
                {
                    databaseName = this.GetDatabaseName();

                    using (var connection = this.GetOpenConnection(databaseName))
                    {
                        executionFlow(connection, test);
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

        protected virtual bool ExecuteNonQuery(IDbConnection connection, string commandText, int timeLimit = DefaultTimeLimit)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = this.FixCommandText(commandText);

                return CodeHelpers.ExecuteWithTimeLimit(
                    TimeSpan.FromMilliseconds(timeLimit),
                    () => command.ExecuteNonQuery());
            }
        }

        protected virtual string FixCommandText(string commandText)
            => commandText;

        protected virtual SqlResult ExecuteReader(
            IDbConnection connection,
            string commandText,
            int timeLimit = DefaultTimeLimit)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;

                using (var reader = command.ExecuteReader())
                {
                    var sqlTestResult = new SqlResult();
                    sqlTestResult.Completed = CodeHelpers.ExecuteWithTimeLimit(
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
            IExecutionContext<TestsInputModel> executionContext,
            TestContext test,
            IExecutionResult<TestResult> result)
        {
            if (sqlResult.Completed)
            {
                var joinedUserOutput = string.Join(Environment.NewLine, sqlResult.Results);

                var checker = executionContext.Input.GetChecker();

                var checkerResult = checker.Check(
                    test.Input,
                    joinedUserOutput,
                    test.Output,
                    test.IsTrialTest);

                result.Results.Add(new TestResult
                {
                    Id = test.Id,
                    ResultType =
                        checkerResult.IsCorrect
                            ? TestRunResultType.CorrectAnswer
                            : TestRunResultType.WrongAnswer,
                    CheckerDetails = checkerResult.CheckerDetails
                });
            }
            else
            {
                result.Results.Add(new TestResult
                {
                    Id = test.Id,
                    TimeUsed = executionContext.TimeLimit,
                    ResultType = TestRunResultType.TimeLimit,
                });
            }
        }
    }
}