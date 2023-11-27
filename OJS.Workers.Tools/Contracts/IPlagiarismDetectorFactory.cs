using OJS.Workers.Tools;

namespace OJS.Services.Worker.Models.Anti_Cheating;

using OJS.Common.Contracts;

public interface IPlagiarismDetectorFactory
{
    IPlagiarismDetector CreatePlagiarismDetector(PlagiarismDetectorCreationContext context);
}