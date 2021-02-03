using System;
using System.Collections.Generic;

namespace ReleaseNotes
{
    internal record WorkItemRecord(string Title, int? Id, string Url, WorkItemType WorkItemType);

    internal record ReleaseContent(string ProjectName, DateTime? StartDate, DateTime? FinishDate, string Version, List<WorkItemRecord> WorkItems);

    internal enum WorkItemType : byte
    {
        Us = 0,
        Bug = 1,
        Feature = 2,
        Epic = 3
    }
}
