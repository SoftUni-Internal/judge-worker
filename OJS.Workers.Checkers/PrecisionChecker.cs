﻿namespace OJS.Workers.Checkers
{
    using System;
    using System.Globalization;

    using OJS.Workers.Common;

    /// <summary>
    /// Checks if each line of decimals are equal with certain precision (default is 14).
    /// </summary>
    public class PrecisionChecker : Checker
    {
        private int precision = 14;

        public override CheckerResult Check(string inputData, string receivedOutput, string expectedOutput, bool isTrialTest)
        {
            var result = this.CheckLineByLine(inputData, receivedOutput, expectedOutput, this.AreEqualWithPrecision, isTrialTest);
            return result;
        }

        public override void SetParameter(string parameter) => this.precision = int.Parse(parameter, CultureInfo.InvariantCulture);

        private bool AreEqualWithPrecision(string userLine, string correctLine)
        {
            try
            {
                userLine = userLine.Replace(',', '.');
                correctLine = correctLine.Replace(',', '.');
                var userLineInNumber = decimal.Parse(userLine, CultureInfo.InvariantCulture);
                var correctLineInNumber = decimal.Parse(correctLine, CultureInfo.InvariantCulture);

                // TODO: Change with 1.0 / math.pow(10, xxx)
                var precisionEpsilon = 1.0m / (decimal)Math.Pow(10, this.precision);

                return Math.Abs(userLineInNumber - correctLineInNumber) < precisionEpsilon;
            }
            catch
            {
                return false;
            }
        }
    }
}
