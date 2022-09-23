using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ConsoleApp2
{
    public class Program
    {
        public static void Main()
        {
            var lines =
                File.ReadAllLines(
                    @"D:\Repos\judge\interactive\Services\Judge\OJS.Workers\mysql-tests-scripts-skeleton\results-CLEAN_UP_DB_STRATEGY_1.txt");

            // Display the file contents by using a foreach loop.
            var list = new List<TimeSpan>();
            System.Console.WriteLine("Contents of WriteLines2.txt = ");
            foreach (var line in lines)
            {
                var pattern = "Time elapsed: ([0-9]+:[0-9]+:[0-9]+.[0-9]+)";
                var match = Regex.Match(line, pattern);
                if (match.Success)
                {
                    list.Add(TimeSpan.Parse(match.Groups[1].Value));
                }
            }

            Console.WriteLine(GetAvarage(list));

            // Keep the console window open in debug mode.
            Console.WriteLine("Press any key to exit.");
        }

        public static TimeSpan GetAvarage(List<TimeSpan> times)
        {
            double doubleAverageTicks = times.Average(timeSpan => timeSpan.Ticks);
            long longAverageTicks = Convert.ToInt64(doubleAverageTicks);

            return new TimeSpan(longAverageTicks);
        }
    }
}