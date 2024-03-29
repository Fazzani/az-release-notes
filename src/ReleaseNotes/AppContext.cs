﻿using System;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Core.WebApi.Types;
using Microsoft.VisualStudio.Services.WebApi;

namespace ReleaseNotes
{
    internal class AppContext
    {
        public const string PAT_NAME = "SYSTEM_ACCESSTOKEN";
        private SemVer _semVersion = null;
        public Uri OrgUrl { get; set; }
        public string VssProjectName { get; set; } = "Up.France.ODI";
        public string ReleaseNotesProjectName { get; set; } = "Uptimise";
        public string ReleaseNotesTeamName { get; set; } = "App - Financeur";
        public string PageReleaseNotePath { get; set; } = "Home/Applications/Uptimise/ReleaseNotes/";

        public TeamContext ReleaseNoteTeamContext
        {
            get
            {
                return new TeamContext(ReleaseNoteProjectReference.Id, ReleaseNoteWebApiTeam.Id);
            }
        }

        public VssConnection VssConnection { get; set; }

        public TeamProjectReference TeamProjectReference { get; set; }
        public WebApiTeam ReleaseNoteWebApiTeam { get; set; }
        public bool DryRun { get; internal set; }
        public string GitRepoName { get; set; } = "Up.France.ODI.Services.Financeur";
        public string ReleaseNoteVersion { get; init; }
        public SemVer SemVersion
        {
            get
            {
                if( _semVersion == null && !string.IsNullOrEmpty(ReleaseNoteVersion))
                    _semVersion = new SemVer(ReleaseNoteVersion);
                return _semVersion;
            }
        }
        public string Query { get; internal set; }
        public string IterationOffset { get; internal set; } = "0";
        public bool Override { get; internal set; }
        public string MajorVersion { get; internal set; }
        public string RepositoryId { get; internal set; }
        public TeamProjectReference ReleaseNoteProjectReference { get; internal set; }
        public bool ForceSelectIterationByTag { get; internal set; } = true;
        public string IterationVersionRegex { get; set; } = @"\s(\d+)$";
    }
}
