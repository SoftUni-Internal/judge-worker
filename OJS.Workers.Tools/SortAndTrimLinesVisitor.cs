﻿namespace OJS.Workers.Tools;

using OJS.Common.Contracts;
using System.Text;

public class SortAndTrimLinesVisitor : IDetectPlagiarismVisitor
{
    public string Visit(string text)
    {
        text = text.Trim();

        var lines = new List<string>();
        using (var stringReader = new StringReader(text!))
        {
            string line;
            while ((line = stringReader.ReadLine() !) != null)
            {
                line = line.Trim();
                lines.Add(line);
            }
        }

        lines.Sort();

        var stringBuilder = new StringBuilder();
        foreach (var line in lines)
        {
            stringBuilder.AppendLine(line);
        }

        return stringBuilder.ToString();
    }
}