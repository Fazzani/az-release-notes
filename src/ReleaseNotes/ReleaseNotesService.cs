using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HandlebarsDotNet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.Work.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;

namespace ReleaseNotes
{
    internal class ReleaseNotesService : IReleaseNotesService
    {
        private readonly ILogger<ReleaseNotesService> _logger;
        private readonly AppOptions _appOption;
        private const string _sprintUrlFormat = "https://dev.azure.com/{0}/{1}/_sprints/taskboard/{2}/{3}";
        private TeamContextFactory _teamContextFactory;
        private (string MantisId, string MantisStatus) MantisColumnNames = ("Custom.b0c854eb-2dcc-46fe-8516-ecbbc703fae9", "Custom.StatutMantis");

        public ReleaseNotesService(ILogger<ReleaseNotesService> logger, IOptions<AppOptions> appAption)
        {
            _logger = logger;
            _appOption = appAption?.Value;
        }

        public async Task UpdateOrCreateReleaseNotesFromCommit(AppContext appContext, CancellationToken cancellationToken = default)
        {
            var witClient = appContext.Connection.GetClient<WorkItemTrackingHttpClient>(cancellationToken);

            try
            {
                appContext.TeamProjectReference = await GetTeamProjectByNameAsync(appContext).ConfigureAwait(false);
                appContext.WebApiTeam = await GetTeamByNameAsync(appContext.Connection,
                                                                 appContext.TeamProjectReference.Id,
                                                                 teamName: appContext.TeamName,
                                                                 cancellationToken).ConfigureAwait(false);

                _teamContextFactory = new TeamContextFactory(appContext.WebApiTeam.Name);

                var gitClient = await appContext.Connection.GetClientAsync<GitHttpClient>(cancellationToken).ConfigureAwait(false);
                var commit = await gitClient.GetCommitAsync(appContext.TeamProjectReference.Id, appContext.CommitId, appContext.RepositoryId, cancellationToken: cancellationToken).ConfigureAwait(false);
                var wiRegex = new Regex(@"(#\d+)", RegexOptions.Multiline);
                var match = wiRegex.Matches(commit.Comment);
                var workItems = match.Select(x => int.Parse(x.Value.Replace("#", string.Empty))).ToList();
                var notes = await GetWorkItemsById(witClient, workItems, cancellationToken).ToListAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogInformation($"{notes.Count} notes is retrieved");

                if (notes.Count > 0)
                {
                    var orgName = appContext.Connection.Uri.Segments[1];
                    //var sprintLink = Uri.EscapeUriString(string.Format(_sprintUrlFormat, orgName, appContext.TeamProjectReference.Name, appContext.TeamName, iter.Path));
                    var pageContent = await GenerateContent(
                        new ReleaseContent(appContext.ReleaseNotesProjectName,
                                           default,
                                           default,
                                           appContext.ReleaseNoteVersion,
                                           default,
                                           _teamContextFactory.GetVelocity(notes),
                                           default,
                                           notes),
                        cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation("A new content generated");

                    if (appContext.DryRun)
                    {
                        Console.WriteLine(pageContent);
                    }
                    else
                    {
                        var pagePath = $"{appContext.PageReleaseNotePath}{appContext.ReleaseNoteVersion}";
                        _logger.LogInformation($"Creating new release notes page at {pagePath}");
                        var (pageResponse, azureAction) = await Wiki.Wiki.GetOrCreateWikiPage(appContext.Connection, appContext.TeamProjectReference.Id, pagePath).ConfigureAwait(false);
                        if (azureAction == Wiki.Wiki.AzureDevopsActionEnum.Update && !appContext.Override)
                            return;
                        var wikiResponse = await Wiki.Wiki.EditWikiPageById(appContext.Connection, appContext.TeamProjectReference.Id, pageResponse.Page.Id.Value, new MemoryStream(Encoding.UTF8.GetBytes(pageContent ?? ""))).ConfigureAwait(false);
                        _logger.LogInformation($"New Release notes page was created here {wikiResponse.Page.RemoteUrl}");
                        Console.WriteLine($"##vso[task.complete result=Succeeded;]{wikiResponse.Page.RemoteUrl}");
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public async Task UpdateOrCreateReleaseNotes(AppContext appContext, CancellationToken cancellationToken = default)
        {
            var witClient = appContext.Connection.GetClient<WorkItemTrackingHttpClient>(cancellationToken);

            try
            {
                appContext.TeamProjectReference = await GetTeamProjectByNameAsync(appContext).ConfigureAwait(false);
                appContext.WebApiTeam = await GetTeamByNameAsync(appContext.Connection,
                                                                 appContext.TeamProjectReference.Id,
                                                                 teamName: appContext.TeamName,
                                                                 cancellationToken).ConfigureAwait(false);

                _teamContextFactory = new TeamContextFactory(appContext.WebApiTeam.Name);

                var selectedIterations = await GetSelectedIterations(appContext, cancellationToken).ConfigureAwait(false);

                foreach (var iter in selectedIterations)
                {
                    var notes = await GetWorkItems(witClient, appContext, iter, cancellationToken)
                        .ToListAsync(cancellationToken).ConfigureAwait(false);

                    _logger.LogInformation($"{notes.Count} notes is retrieved");

                    if (notes.Count > 0)
                    {
                        var version = GetVersionBySprintStrategy(appContext, iter, appContext.MajorVersion);
                        var orgName = appContext.Connection.Uri.Segments[1];
                        var sprintLink = Uri.EscapeUriString(string.Format(_sprintUrlFormat, orgName, appContext.TeamProjectReference.Name, appContext.TeamName, iter.Path));
                        var pageContent = await GenerateContent(
                            new ReleaseContent(appContext.ReleaseNotesProjectName,
                                               iter.Attributes.StartDate,
                                               iter.Attributes.FinishDate,
                                               version,
                                               iter.Name,
                                               _teamContextFactory.GetVelocity(notes),
                                               sprintLink,
                                               notes),
                            cancellationToken).ConfigureAwait(false);
                        _logger.LogInformation("A new content generated");

                        if (appContext.DryRun)
                        {
                            Console.WriteLine(pageContent);
                        }
                        else
                        {
                            var pagePath = $"{appContext.PageReleaseNotePath}{version}";
                            _logger.LogInformation($"Creating new release notes page at {pagePath}");
                            var (pageResponse, azureAction) = await Wiki.Wiki.GetOrCreateWikiPage(appContext.Connection, appContext.TeamProjectReference.Id, pagePath).ConfigureAwait(false);
                            if (azureAction == Wiki.Wiki.AzureDevopsActionEnum.Update && !appContext.Override)
                                return;
                            var wikiResponse = await Wiki.Wiki.EditWikiPageById(appContext.Connection, appContext.TeamProjectReference.Id, pageResponse.Page.Id.Value, new MemoryStream(Encoding.UTF8.GetBytes(pageContent ?? ""))).ConfigureAwait(false);
                            _logger.LogInformation($"New Release notes page was created here {wikiResponse.Page.RemoteUrl}");
                            Console.WriteLine($"##vso[task.complete result=Succeeded;]{wikiResponse.Page.RemoteUrl}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        private async Task<TeamSettingsIteration[]> GetSelectedIterations(AppContext appContext,
                                                                          CancellationToken cancellationToken)
        {
            var allIterations = await GetIterationsByProjectAsync(appContext, cancellationToken).ConfigureAwait(false);
            var selectedIter = allIterations.ToArray();

            if (Int32.TryParse(appContext.IterationOffset, out int iterIndex))
            {
                selectedIter = new[] { GetIterationByIndex(iterIndex, allIterations) };
            }
            else
            {
                var m = Regex.Match(appContext.IterationOffset, @"^(\d+)\.\.(\d)*");
                if (m.Success)
                {
                    if (string.IsNullOrEmpty(m.Groups[2].Value))
                    {
                        selectedIter = selectedIter[new Range(Int32.Parse(m.Groups[1].Value), ^1)];
                    }
                    else
                    {
                        selectedIter = selectedIter[new Range(Int32.Parse(m.Groups[1].Value), Int32.Parse(m.Groups[2].Value))];
                    }
                }
            }

            return selectedIter;
        }

        public string GetVersionBySprintStrategy(AppContext appContext,
                                                 TeamSettingsIteration iteration,
                                                 string versionPrefix)
        {
            if (!string.IsNullOrEmpty(appContext.ReleaseNoteVersion))
            {
                return appContext.ReleaseNoteVersion;
            }

            var sprint = new Regex(@"\d+$").Match(iteration.Name);
            if (sprint.Success)
                return $"{versionPrefix}.{sprint.Value}.0";

            return appContext.ReleaseNoteVersion;
        }

        public async Task<string> GenerateContent(ReleaseContent releaseContent, CancellationToken cancellationToken = default)
        {
            var hbs = await File
                .ReadAllTextAsync(Path.Join(AppDomain.CurrentDomain.BaseDirectory, _teamContextFactory.GetHbsTemplateName()), cancellationToken)
                .ConfigureAwait(false);
            var tpl = Handlebars.Compile(hbs);
            var data = _teamContextFactory.GetContentData(releaseContent);
            return tpl(data);
        }

        public async IAsyncEnumerable<WorkItemRecord> GetWorkItems(WorkItemTrackingHttpClient witClient,
                                                                   AppContext appContext,
                                                                   TeamSettingsIteration iter,
                                                                   [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var q = await GetQuery(witClient, appContext, iter, cancellationToken).ConfigureAwait(false);
            var res = await witClient.QueryByWiqlAsync(q, appContext.TeamContext, top: 100, cancellationToken: cancellationToken).ConfigureAwait(false);

            foreach (var item in res.WorkItems)
            {
                var workitem = await witClient.GetWorkItemAsync(item.Id, cancellationToken: cancellationToken).ConfigureAwait(false);
                var originalEstimate = 0;
                var storyPoint = 0;

                if (workitem.Fields.TryGetValue("Microsoft.VSTS.Scheduling.OriginalEstimate", out var originalEstimateValue))
                {
                    originalEstimate = Convert.ToInt32(originalEstimateValue);
                }

                if (workitem.Fields.TryGetValue("Microsoft.VSTS.Scheduling.StoryPoints", out var storyPointValue))
                {
                    storyPoint = Convert.ToInt32(storyPointValue);
                }

                yield return new WorkItemRecord(workitem.Fields["System.Title"].ToString(),
                    workitem.Id,
                    workitem.Links.Links["html"] is ReferenceLink link ? link.Href : string.Empty,
                    Extensions.WorkItemTypeFromString(workitem.Fields["System.WorkItemType"].ToString()),
                    originalEstimate,
                    storyPoint,
                    workitem.Fields["System.BoardColumn"].ToString(),
                    workitem.Fields.ContainsKey(MantisColumnNames.MantisStatus) && !string.IsNullOrEmpty(workitem.Fields[MantisColumnNames.MantisStatus].ToString()),
                    workitem.Fields.ContainsKey(MantisColumnNames.MantisId) ? workitem.Fields[MantisColumnNames.MantisId].ToString() : string.Empty);
            }
        }

        public async IAsyncEnumerable<WorkItemRecord> GetWorkItemsById(WorkItemTrackingHttpClient witClient,
                                                                       List<int> workitemsId,
                                                                       [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var item in workitemsId)
            {
                var workitem = await witClient.GetWorkItemAsync(item, cancellationToken: cancellationToken).ConfigureAwait(false);
                var originalEstimate = 0;
                var storyPoint = 0;

                if (workitem.Fields.TryGetValue("Microsoft.VSTS.Scheduling.OriginalEstimate", out var originalEstimateValue))
                {
                    originalEstimate = Convert.ToInt32(originalEstimateValue);
                }

                if (workitem.Fields.TryGetValue("Microsoft.VSTS.Scheduling.StoryPoints", out var storyPointValue))
                {
                    storyPoint = Convert.ToInt32(storyPointValue);
                }

                yield return new WorkItemRecord(workitem.Fields["System.Title"].ToString(),
                    workitem.Id,
                    workitem.Links.Links["html"] is ReferenceLink link ? link.Href : string.Empty,
                    Extensions.WorkItemTypeFromString(workitem.Fields["System.WorkItemType"].ToString()),
                    originalEstimate,
                    storyPoint,
                    workitem.Fields.ContainsKey("System.BoardColumn") ? workitem.Fields["System.BoardColumn"].ToString() : string.Empty,
                    workitem.Fields.ContainsKey(MantisColumnNames.MantisStatus) && !string.IsNullOrEmpty(workitem.Fields[MantisColumnNames.MantisStatus].ToString()),
                    workitem.Fields.ContainsKey(MantisColumnNames.MantisId) ? workitem.Fields[MantisColumnNames.MantisId].ToString() : string.Empty);
            }
        }

        private async ValueTask<Wiql> GetQuery(WorkItemTrackingHttpClient witClient,
                                               AppContext appContext,
                                               TeamSettingsIteration iter,
                                               CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(appContext.Query))
                return new Wiql { Query = _appOption.Query };

            var q = await witClient.GetQueryAsync(appContext.TeamProjectReference.Id, appContext.Query, QueryExpand.All, 1, cancellationToken: cancellationToken).ConfigureAwait(false);
            var query = Regex.Replace(q.Wiql, @"\[System\.IterationPath\]\s=\s'[^']+'", _ => $"[System.IterationPath] = '{iter.Path}'", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            return new Wiql { Query = query };
        }

        public async Task<TeamProjectReference> GetTeamProjectByNameAsync(AppContext appContext)
        {
            var projectClient = appContext.Connection.GetClient<ProjectHttpClient>();
            var projects = await projectClient.GetProjects(ProjectState.All).ConfigureAwait(false);
            while (projects.All(x => x.Name != appContext.VssProjectName && !string.IsNullOrEmpty(projects.ContinuationToken)))
            {
                projects = await projectClient.GetProjects(ProjectState.All, continuationToken: projects.ContinuationToken).ConfigureAwait(false);
            }
            return projects.FirstOrDefault(x => x.Name == appContext.VssProjectName);
        }

        public static async Task<WebApiTeam> GetTeamByNameAsync(VssConnection connection,
                                                                Guid projectId,
                                                                string teamName,
                                                                CancellationToken cancellationToken)
        {
            var teamClient = connection.GetClient<TeamHttpClient>(cancellationToken);
            var teams = await teamClient.GetTeamsAsync(projectId.ToString(), cancellationToken: cancellationToken).ConfigureAwait(false);
            return teams.FirstOrDefault(x => x.Name == teamName);
        }

        public Task<List<TeamSettingsIteration>> GetIterationsByProjectAsync(AppContext appContext,
                                                                                   CancellationToken cancellationToken = default)
        {
            var client = appContext.Connection.GetClient<WorkHttpClient>(cancellationToken);
            return client.GetTeamIterationsAsync(appContext.TeamContext, cancellationToken: cancellationToken);
        }

        private static TeamSettingsIteration GetIterationByIndex(int iterationOffset, List<TeamSettingsIteration> iterations)
        {
            if (iterationOffset == 0)
            {
                return iterations.FirstOrDefault(x => x.Attributes.TimeFrame == TimeFrame.Current);
            }
            if (iterationOffset < 0)
            {
                return iterations.Where(x => x.Attributes.TimeFrame == TimeFrame.Past).ToArray()[^Math.Abs(iterationOffset)];
            }
            if (iterationOffset > 0)
            {
                return iterations.Where(x => x.Attributes.TimeFrame == TimeFrame.Future).ToArray()[iterationOffset - 1];
            }
            return default;
        }
    }
}
