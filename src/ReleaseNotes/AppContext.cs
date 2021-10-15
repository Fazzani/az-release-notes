using System;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Core.WebApi.Types;
using Microsoft.VisualStudio.Services.WebApi;

namespace ReleaseNotes
{
    internal class AppContext
    {
        public const string PAT_NAME = "SYSTEM_ACCESSTOKEN";

        public Uri OrgUrl { get; set; }
        public string VssProjectName { get; set; } = "Up.France.ODI";
        public string ReleaseNotesProjectName { get; set; } = "Uptimise";
        public string TeamName { get; set; } = "App - Financeur";
        public string PageReleaseNotePath { get; set; } = "Home/Applications/Uptimise/ReleaseNotes/";
        public TeamContext TeamContext
        {
            get
            {
                return new TeamContext(TeamProjectReference.Id, WebApiTeam.Id);
            }
        }
        public VssConnection WikiConnection { get; set; }
        public VssConnection VssConnection { get; set; }

        public TeamProjectReference TeamProjectReference { get; set; }
        public WebApiTeam WebApiTeam { get; set; }
        public bool DryRun { get; internal set; }

        public string GitRepoName { get; set; } = "Up.France.ODI.Services.Financeur";
        public string ReleaseNoteVersion { get; internal set; }
        public string Query { get; internal set; }
        public string IterationOffset { get; internal set; } = "0";
        public bool Override { get; internal set; }
        public string MajorVersion { get; internal set; }
        public Guid RepositoryId { get; internal set; }
        public TeamProjectReference ReleaseNoteProjectReference { get; internal set; }
    }
}
