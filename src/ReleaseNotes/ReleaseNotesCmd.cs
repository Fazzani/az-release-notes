using System;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.Common;
using System.Threading;

namespace ReleaseNotes
{
    [Command(
        Name = "rnotes",
        OptionsComparison = StringComparison.InvariantCultureIgnoreCase,
        Description = "rnotes CLI help us to generate Release notes from azure devops")]
    [HelpOption("--help")]
    [VersionOptionFromMember("--version", MemberName = nameof(GetVersion))]
    internal class ReleaseNotesCmd
    {
        private readonly ILogger<ReleaseNotesCmd> _logger;
        private readonly IReleaseNotesService _releaseNotesService;

        public ReleaseNotesCmd(ILogger<ReleaseNotesCmd> logger, IReleaseNotesService releaseNotesService)
        {
            _logger = logger;
            _releaseNotesService = releaseNotesService;
        }

        public async Task<int> OnExecuteAsync(CommandLineApplication app, CancellationToken cancellationToken = default)
        {
            if (!Uri.TryCreate(OrgUrl, UriKind.Absolute, out var uri))
            {
                _logger.LogError($"Invalid organization url '{OrgUrl}'");
                app.ShowHelp();
                return 1;
            }

            var appContext = new AppContext
            {
                OrgUrl = uri,
                TeamName = TeamName,
                PageReleaseNotePath = PageReleaseNotePath,
                VssProjectName = VssProjectName,
                ReleaseNotesProjectName = ReleaseNotesProjectName,
                ReleaseNoteVersion = ReleaseNoteVersion,
                Query = Query,
                DryRun = DryRun,
                IterationOffset = IterationOffset,
                Override = Override,
                MajorVersion = SemverMajorVersion,
                RepositoryId = string.IsNullOrEmpty(RepositoryId) ? Guid.Empty : Guid.Parse(RepositoryId)
            };

            // Create a connection
            appContext.VssConnection = new VssConnection(appContext.OrgUrl, new VssBasicCredential(string.Empty, PAT));

            if (!string.IsNullOrEmpty(appContext.ReleaseNoteVersion))
                await _releaseNotesService.UpdateOrCreateReleaseNotesFromCommit(appContext, cancellationToken).ConfigureAwait(false);
            else
                await _releaseNotesService.UpdateOrCreateReleaseNotes(appContext, cancellationToken).ConfigureAwait(false);
            return 0;
        }

        [Option("-o|--organization", "Azure devops organization url", CommandOptionType.SingleValue)]
        public string OrgUrl { get; } = Environment.GetEnvironmentVariable("SYSTEM_COLLECTIONURI");

        [Option("-p|--project", "Azure devops project name", CommandOptionType.SingleValue)]
        public string VssProjectName { get; set; } = Environment.GetEnvironmentVariable("SYSTEM_TEAMPROJECT");

        [Option(Description = "Azure devops wiki project name", ShortName = "r")]
        public string ReleaseNotesProjectName { get; set; }

        [Option("-t|--team", "Wiki team name", CommandOptionType.SingleValue)]
        public string TeamName { get; set; }

        [Option(Description = "Personal access token", ShortName = "x")]
        public string PAT { get; set; } = Environment.GetEnvironmentVariable(AppContext.PAT_NAME);

        [Option(Description = "Release notes page path", ShortName = "n")]
        public string PageReleaseNotePath { get; set; }

        [Option("-q|--query", "Query Id Or Path: used to retreive release notes work items", CommandOptionType.SingleValue)]
        public string Query { get; set; }

        [Option("-rv|--relver", "Overrite release notes version", CommandOptionType.SingleValue)]
        public string ReleaseNoteVersion { get; set; }

        [Option("-mv|--majorVersion", "Semver Major version", CommandOptionType.SingleValue)]
        public string SemverMajorVersion { get; set; } = "v2";

        [Option("-i|--iteration", "Iteration offset (ex: +1, -1). The arg maybe be a rang (ex: .., 1..3) ", CommandOptionType.SingleValue)]
        public string IterationOffset { get; set; } = "0";

        [Option("-d|--dry", "If true, the generated release content will be displayed on console", CommandOptionType.NoValue)]
        public bool DryRun { get; set; } = false;

        [Option("-f|--force", "Force recreate existed wiki pages", CommandOptionType.NoValue)]
        public bool Override { get; set; } = false;

        [Option("-repo|--repositoryId", "Repository Id", CommandOptionType.SingleValue)]
        public string RepositoryId { get; set; } = Environment.GetEnvironmentVariable("REPOSITORY_ID");

        private static string GetVersion()
            => $"v{typeof(ReleaseNotesCmd).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion}";
    }
}
