﻿using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Work.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;

namespace ReleaseNotes
{
    internal interface IReleaseNotesService
    {
        Task<string> GenerateContent(ReleaseContent releaseContent, CancellationToken cancellationToken = default);
        Task<TeamProjectReference> GetTeamProjectByNameAsync(ProjectHttpClient projectClient, string projectName);
        IAsyncEnumerable<WorkItemRecord> GetWorkItems(WorkItemTrackingHttpClient witClient, AppContext appContext, TeamSettingsIteration iter, [EnumeratorCancellation] CancellationToken cancellationToken);
        Task UpdateOrCreateReleaseNotes(AppContext appContext, CancellationToken cancellationToken = default);
        Task<List<TeamSettingsIteration>> GetIterationsByProjectAsync(AppContext appContext,
                                                     CancellationToken cancellationToken = default);
        Task UpdateOrCreateReleaseNotesFromCommit(AppContext appContext, CancellationToken cancellationToken = default);
    }
}