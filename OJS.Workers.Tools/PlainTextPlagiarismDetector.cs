﻿namespace OJS.Workers.Tools;

using OJS.Common.Contracts;
using OJS.Common.Models;
using OJS.Services.Worker.Models.Anti_Cheating;

public class PlainTextPlagiarismDetector : IPlagiarismDetector
{
    private readonly ISimilarityFinder similarityFinder;

    public PlainTextPlagiarismDetector(ISimilarityFinder similarityFinder) => this.similarityFinder = similarityFinder;

    // TODO: This method is very similar to CSharpCompileDecompilePlagiarismDetector.DetectPlagiarism
    public PlagiarismResult DetectPlagiarism(
        string firstSource,
        string secondSource,
        IEnumerable<IDetectPlagiarismVisitor>? visitors = null)
    {
        if (visitors != null)
        {
            foreach (var visitor in visitors)
            {
                firstSource = visitor.Visit(firstSource);
                secondSource = visitor.Visit(secondSource);
            }
        }

        var differences = this.similarityFinder.DiffText(firstSource, secondSource, true, true, true);

        var differencesCount = differences.Sum(difference => difference.DeletedA + difference.InsertedB);
        var textLength = firstSource.Length + secondSource.Length;

        // TODO: Revert the percentage
        var percentage = ((decimal)differencesCount * 100) / textLength;

        return new PlagiarismResult(percentage)
        {
            Differences = differences,
            FirstToCompare = firstSource,
            SecondToCompare = secondSource,
        };
    }
}