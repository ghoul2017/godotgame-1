using System.Collections.Generic;
using System.Linq;

namespace GodotGame;

public sealed class DataLoadReport
{
    public List<DataLoadIssue> Issues { get; } = new();
    public bool HasFatalErrors => Issues.Any(issue => issue.Status == DefinitionStatus.FatalError);

    public void Add(DefinitionStatus status, string message)
    {
        Issues.Add(new DataLoadIssue
        {
            Status = status,
            Message = message
        });
    }
}
