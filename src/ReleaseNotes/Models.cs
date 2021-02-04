using System;
using System.Collections.Generic;

namespace ReleaseNotes
{
    internal record WorkItemRecord(string Title, int? Id, string Url, WorkItemType WorkItemType, int OriginalEstimated);

    internal record ReleaseContent(string ProjectName, DateTime? StartDate, DateTime? FinishDate, string Version,
        string IterationName,
        int Velocity,
        string SprintLink,
        List<WorkItemRecord> WorkItems);

    internal enum WorkItemType : byte
    {
        Us = 0,
        Bug = 1,
        Feature = 2,
        Epic = 3
    }
}
