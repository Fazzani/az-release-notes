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

        public ReleaseNotesService(ILogger<ReleaseNotesService> logger, IOptions<AppOptions> appAption)
        {
            _logger = logger;
            _appOption = appAption?.Value;
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

                var selectedIterations = await GetSelectedIterations(appContext, cancellationToken).ConfigureAwait(false);

                foreach (var iter in selectedIterations)
                {
                    var notes = await GetWorkItems(witClient, appContext, iter, cancellationToken)
                        .ToListAsync(cancellationToken).ConfigureAwait(false);

                    _logger.LogInformation($"{notes.Count} notes was retreived");

                    if (notes.Count > 0)
                    {
                        var version = GetVersionBySprintStrategy(appContext, iter);
                        var pageContent = await GenerateContent(notes, appContext.ReleaseNotesProjectName, version, cancellationToken).ConfigureAwait(false);
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
                            _logger.LogInformation($"New Release notes page created here {wikiResponse.Page.RemoteUrl}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        private async Task<TeamSettingsIteration[]> GetSelectedIterations(AppContext appContext, CancellationToken cancellationToken)
        {
            var allIterations = await GetIterationsByProjectAsync(appContext, cancellationToken).ConfigureAwait(false);

            var selectedIter = allIterations.ToArray();

            if (Int32.TryParse(appContext.IterationOffset, out int iterIndex))
            {
                selectedIter = new[] { GetIterationByIndice(iterIndex, allIterations) };
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
                                                  string versionSuffix = "v2.")
        {
            if (!string.IsNullOrEmpty(appContext.ReleaseNoteVersion))
            {
                return appContext.ReleaseNoteVersion;
            }

            var sprint = new Regex(@"\d+$").Match(iteration.Name);
            if (sprint.Success)
                return $"{versionSuffix}{sprint.Value}.0";

            return appContext.ReleaseNoteVersion;
        }

        public async ValueTask<string> GetVersionByTagStrategy(AppContext appContext,
                                                  TeamSettingsIteration iteration,
                                                  string versionSuffix = "v2.",
                                                  CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(appContext.ReleaseNoteVersion))
            {
                return appContext.ReleaseNoteVersion;
            }

            var gitClient = await appContext.Connection.GetClientAsync<GitHttpClient>(cancellationToken).ConfigureAwait(false);
            var repos = await gitClient.GetRepositoriesAsync(appContext.TeamProjectReference.Id, cancellationToken: cancellationToken).ConfigureAwait(false);

            var repo = repos.FirstOrDefault(x => x.Name.Equals(appContext.GitRepoName, StringComparison.OrdinalIgnoreCase));
            if (repo == null)
            {
                return appContext.ReleaseNoteVersion;
            }
            var tags = await gitClient.GetTagRefsAsync(repo.Id, cancellationToken).ConfigureAwait(false);
            var tag = tags.LastOrDefault();

            if (iteration.Attributes.FinishDate.HasValue)
            {
                for (int i = 1; i < tags.Count; i++)
                {
                    tag = tags[^i];
                    var annotatedTag = await gitClient.GetAnnotatedTagAsync(appContext.TeamContext.ProjectId.Value, repo.Id, tag.ObjectId, cancellationToken: cancellationToken).ConfigureAwait(false);
                    if (annotatedTag.TaggedBy.Date <= iteration.Attributes.FinishDate)
                    {
                        if (annotatedTag.TaggedBy.Date <= iteration.Attributes.StartDate)
                        {
                            _logger.LogWarning($"No git tag was founded in this timeframe {iteration.Attributes.StartDate} {iteration.Attributes.FinishDate}");
                            //calculate version from iteration
                            var sprint = new Regex(@"\d+$").Match(iteration.Name);
                            if (sprint.Success)
                                return $"{versionSuffix}{sprint.Value}.0";
                        }
                        break;
                    }
                }
            }
            return tag.Name.Split('/').LastOrDefault();
        }

        public async Task<string> GenerateContent(List<WorkItemRecord> notes,
                                                  string projectName = "Uptimise",
                                                  string version = "1.0.0",
                                                  CancellationToken cancellationToken = default)
        {
            Handlebars.RegisterTemplate("Note", _appOption.NoteTpl);
            var hbs = await File.ReadAllTextAsync("release.hbs", cancellationToken).ConfigureAwait(false);
            var tpl = Handlebars.Compile(hbs);
            var data = new
            {
                ProjectName = projectName,
                Date = DateTime.Now.ToShortDateString(),
                Version = version,
                Features = notes.Where(x => x.WorkItemType == WorkItemType.Us),
                Bugs = notes.Where(x => x.WorkItemType == WorkItemType.Bug),
            };
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
                yield return new WorkItemRecord(workitem.Fields["System.Title"].ToString(),
                    workitem.Id,
                    workitem.Links.Links["html"] is ReferenceLink link ? link.Href : string.Empty,
                    Extensions.WorkItemTypeFromString(workitem.Fields["System.WorkItemType"].ToString()));
            }
        }

        private async ValueTask<Wiql> GetQuery(WorkItemTrackingHttpClient witClient, AppContext appContext, TeamSettingsIteration iter, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(appContext.Query))
                return new Wiql { Query = _appOption.Query };

            var q = await witClient.GetQueryAsync(appContext.TeamProjectReference.Id, appContext.Query, QueryExpand.All, 1, cancellationToken: cancellationToken).ConfigureAwait(false);
            var query = Regex.Replace(q.Wiql, @"\[System\.IterationPath\]\s=\s'[^']+'", match => $"[System.IterationPath] = '{iter.Path}'", RegexOptions.Multiline | RegexOptions.IgnoreCase);
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
            var teamClient = connection.GetClient<TeamHttpClient>();
            var teams = await teamClient.GetTeamsAsync(projectId.ToString(), cancellationToken: cancellationToken).ConfigureAwait(false);
            return teams.FirstOrDefault(x => x.Name == teamName);
        }

        public async Task<List<TeamSettingsIteration>> GetIterationsByProjectAsync(AppContext appContext,
                                                                                   CancellationToken cancellationToken = default)
        {
            var client = appContext.Connection.GetClient<WorkHttpClient>();
            return await client.GetTeamIterationsAsync(appContext.TeamContext,
                                                                    cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private static TeamSettingsIteration GetIterationByIndice(int iterationOffset, List<TeamSettingsIteration> iterations)
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
