using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;

namespace ReleaseNotes
{
    internal record WorkItemRecord(string Title, int? Id, string Url, WorkItemType WorkItemType, int StoryPoint, string BoradColumn, bool IsMantis, string MantisId);

    internal record ReleaseContent(
        string ProjectName,
        DateTime? StartDate,
        DateTime? FinishDate,
        string Version,
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

    internal static class Extension
    {
        public static WorkItemRecord AzWorkItemToWorkItemRecord(this WorkItem workitem, (string MantisId, string MantisStatus) mantisColumnNames)
        {
            var storyPoint = 0;
            if (workitem.Fields.TryGetValue("Microsoft.VSTS.Scheduling.StoryPoints", out var storyPointValue))
            {
                storyPoint = Convert.ToInt32(storyPointValue);
            }

            return new WorkItemRecord(
                workitem.Fields["System.Title"].ToString(),
                workitem.Id,
                workitem.Links.Links["html"] is ReferenceLink link ? link.Href : string.Empty,
                Extensions.WorkItemTypeFromString(workitem.Fields["System.WorkItemType"].ToString()),
                storyPoint,
                workitem.Fields["System.BoardColumn"].ToString(),
                workitem.Fields.ContainsKey(mantisColumnNames.MantisStatus) && !string.IsNullOrEmpty(workitem.Fields[mantisColumnNames.MantisStatus].ToString()),
                workitem.Fields.ContainsKey(mantisColumnNames.MantisId) ? workitem.Fields[mantisColumnNames.MantisId].ToString() : string.Empty);
        }
    }
}
