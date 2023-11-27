﻿namespace OJS.Workers.Tools;

using OJS.Services.Worker.Models.Anti_Cheating;
using OJS.Workers.Common.Models;


public class PlagiarismDetectorCreationContext
{
    public PlagiarismDetectorCreationContext(PlagiarismDetectorType type, ISimilarityFinder similarityFinder)
    {
        if (similarityFinder == null)
        {
            throw new ArgumentNullException(nameof(similarityFinder));
        }

        this.Type = type;
        this.SimilarityFinder = similarityFinder;
    }

    public PlagiarismDetectorType Type { get; set; }

    public string? CompilerPath { get; set; }

    public string? DisassemblerPath { get; set; }

    public ISimilarityFinder SimilarityFinder { get; set; }
}