using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReleaseNotes
{
    internal  class TeamContextFactory
    {
        internal const string BoardColumnNameDone = "Done";
        private readonly bool _isBMC;

        public TeamContextFactory(string teamName="")
        {

            _isBMC = teamName.Equals("App - Bureau Metier", StringComparison.InvariantCultureIgnoreCase);
        }

        internal  int GetVelocity(IEnumerable<WorkItemRecord> notes)
        {
            return _isBMC ? notes.Where(x => x.BoradColumn.Equals(BoardColumnNameDone)).Sum(x => x.StoryPoint) :
                notes.Sum(x => x.OriginalEstimated);
        }

        internal string GetHbsTemplateName()
        {
            return _isBMC ? "releaseBmc.hbs" : "release.hbs";
        }

        internal object GetContentDate(ReleaseContent releaseContent)
        {
            if (_isBMC)
            {
                return  new
                {
                    releaseContent.ProjectName,
                    StartDate = releaseContent.StartDate.GetValueOrDefault(DateTime.Now).ToShortDateString(),
                    FinishDate = releaseContent.FinishDate.GetValueOrDefault(DateTime.Now).ToShortDateString(),
                    releaseContent.Version,
                    releaseContent.IterationName,
                    releaseContent.Velocity,
                    releaseContent.SprintLink,
                    Features = releaseContent.WorkItems.Where(x => x.WorkItemType == WorkItemType.Us && x.BoradColumn.Equals(BoardColumnNameDone)).Select(x => x.Id),
                    UatBugs = releaseContent.WorkItems.Where(x => x.WorkItemType == WorkItemType.Bug && x.BoradColumn.Equals(BoardColumnNameDone) && x.IsMantis).Select(x => x.Id),
                    OthersBugs = releaseContent.WorkItems.Where(x => x.WorkItemType == WorkItemType.Bug && x.BoradColumn.Equals(BoardColumnNameDone) && !x.IsMantis).Select(x => x.Id),
                    PreviewFeatures = releaseContent.WorkItems.Where(x => x.WorkItemType == WorkItemType.Us && !x.BoradColumn.Equals(BoardColumnNameDone)).Select(x => x.Id),
                    PreviewBugs = releaseContent.WorkItems.Where(x => x.WorkItemType == WorkItemType.Bug && !x.BoradColumn.Equals(BoardColumnNameDone) && !x.IsMantis).Select(x => x.Id),
                };
            }
            return new
            {
                releaseContent.ProjectName,
                StartDate = releaseContent.StartDate.GetValueOrDefault(DateTime.Now).ToShortDateString(),
                FinishDate = releaseContent.FinishDate.GetValueOrDefault(DateTime.Now).ToShortDateString(),
                releaseContent.Version,
                releaseContent.IterationName,
                releaseContent.Velocity,
                releaseContent.SprintLink,
                Features = releaseContent.WorkItems.Where(x => x.WorkItemType == WorkItemType.Us).Select(x => x.Id),
                Bugs = releaseContent.WorkItems.Where(x => x.WorkItemType == WorkItemType.Bug).Select(x => x.Id),
            };
        }
    }
}
