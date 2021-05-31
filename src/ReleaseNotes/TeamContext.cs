using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ReleaseNotes
{
    internal class TeamContextFactory
    {
        internal const string BoardColumnNameDone = "Done";
        private readonly bool _isBMC;

        public TeamContextFactory(string teamName = "")
        {
            _isBMC = teamName.Contains("BMC", StringComparison.InvariantCultureIgnoreCase);
        }

        internal int GetVelocity(IEnumerable<WorkItemRecord> notes)
        {
            return _isBMC ? notes.Where(x => x.BoradColumn.Equals(BoardColumnNameDone)).Sum(x => x.StoryPoint) :
                notes.Sum(x => x.StoryPoint);
        }

        internal string GetHbsTemplateName()
        {
            return _isBMC ? "releaseBmc.hbs" : "release.hbs";
        }

        internal object GetContentData(ReleaseContent releaseContent)
        {
            var frCulture = CultureInfo.CreateSpecificCulture("fr-FR");

            if (_isBMC)
            {
                return new
                {
                    releaseContent.ProjectName,
                    StartDate = releaseContent.StartDate.GetValueOrDefault(DateTime.Now).ToString("d", frCulture),
                    FinishDate = releaseContent.FinishDate.GetValueOrDefault(DateTime.Now).ToString("d", frCulture),
                    releaseContent.Version,
                    releaseContent.IterationName,
                    releaseContent.Velocity,
                    releaseContent.SprintLink,
                    TotalItems = releaseContent.WorkItems.Count(x => x.BoradColumn.Equals(BoardColumnNameDone)),
                    Features = releaseContent.WorkItems.Where(x => x.WorkItemType == WorkItemType.Us && x.BoradColumn.Equals(BoardColumnNameDone)).Select(x => x.Id).ToList(),
                    UatBugs = releaseContent.WorkItems.Where(x => x.WorkItemType == WorkItemType.Bug && x.BoradColumn.Equals(BoardColumnNameDone) && x.IsMantis).Select(x => new { x.Id, x.MantisId }).ToList(),
                    OthersBugs = releaseContent.WorkItems.Where(x => x.WorkItemType == WorkItemType.Bug && x.BoradColumn.Equals(BoardColumnNameDone) && !x.IsMantis).Select(x => x.Id).ToList(),
                    PreviewFeatures = releaseContent.WorkItems.Where(x => x.WorkItemType == WorkItemType.Us && !x.BoradColumn.Equals(BoardColumnNameDone)).Select(x => x.Id).ToList(),
                    PreviewBugs = releaseContent.WorkItems.Where(x => x.WorkItemType == WorkItemType.Bug && !x.BoradColumn.Equals(BoardColumnNameDone) && !x.IsMantis).Select(x => x.Id).ToList(),
                    PreviewOthersBugs = releaseContent.WorkItems.Where(x => x.WorkItemType == WorkItemType.Bug && !x.BoradColumn.Equals(BoardColumnNameDone) && !x.IsMantis).Select(x => x.Id).ToList(),
                };
            }
            return new
            {
                releaseContent.ProjectName,
                StartDate = releaseContent.StartDate.GetValueOrDefault(DateTime.Now).ToString("d", frCulture),
                FinishDate = releaseContent.FinishDate.GetValueOrDefault(DateTime.Now).ToString("d", frCulture),
                releaseContent.Version,
                releaseContent.IterationName,
                releaseContent.Velocity,
                releaseContent.SprintLink,
                TotalItems = releaseContent.WorkItems.Count,
                Features = releaseContent.WorkItems.Where(x => x.WorkItemType == WorkItemType.Us).Select(x => x.Id).ToList(),
                Bugs = releaseContent.WorkItems.Where(x => x.WorkItemType == WorkItemType.Bug).Select(x => x.Id).ToList(),
            };
        }
    }
}
