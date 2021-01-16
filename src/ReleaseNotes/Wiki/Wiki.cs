using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.Wiki.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace ReleaseNotes.Wiki
{
    public static class Wiki
    {
        public enum AzureDevopsActionEnum : byte
        {
            None = 0,
            Create = 1,
            Update = 2
        }

        public static async Task<(WikiPageResponse, AzureDevopsActionEnum)> GetOrCreateWikiPage(VssConnection connection, Guid projectId, string pagePath = "ReleaseNotes")
        {
            var (wiki, _) = Helpers.FindProjectWiki(connection, projectId);
            if (wiki == null)
                throw new InvalidOperationException($"Wiki project {projectId} not found");
            var wikiClient = connection.GetClient<WikiHttpClient>();
            try
            {
                return (await wikiClient.GetPageAsync(projectId, wiki.Id, pagePath).ConfigureAwait(false), AzureDevopsActionEnum.Update);
            }
            catch
            {
                Console.WriteLine($"Page not found {pagePath}");
            }
            var wikiPageResponse = Helpers.CreatePage(connection, wiki, pagePath);
            Console.WriteLine("Create page '{0}' in wiki '{1}'", wikiPageResponse.Page.Path, wiki.Name);
            return (wikiPageResponse, AzureDevopsActionEnum.Create);
        }

        public static WikiPageResponse CreateWikiPage(VssConnection connection, Guid projectId, string pageName)
        {
            var wiki = Helpers.FindOrCreateProjectWiki(connection, projectId);
            var wikiPageResponse = Helpers.CreatePage(connection, wiki, pageName);
            Console.WriteLine("Create page '{0}' in wiki '{1}'", wikiPageResponse.Page.Path, wiki.Name);
            return wikiPageResponse;
        }

        public static async Task<WikiPageResponse> EditWikiPageById(VssConnection connection, Guid projectId, int pageId, Stream stream)
        {
            var wikiClient = connection.GetClient<WikiHttpClient>();

            var wiki = Helpers.FindOrCreateProjectWiki(connection, projectId);

            var pageResponse = await wikiClient.GetPageByIdAsync(
                project: wiki.ProjectId,
                wikiIdentifier: wiki.Name,
                id: pageId,
                includeContent: true).ConfigureAwait(false);

            var somePage = pageResponse.Page;

            Console.WriteLine("Retrieved page with Id '{0}' as JSON in wiki '{1}' with content '{2}'", somePage.Id, wiki.Name, somePage.Content);

            var originalContent = somePage.Content;
            var originalVersion = pageResponse.ETag.ToList()[0];

            using var reader = new StreamReader(stream);
            var parameters = new WikiPageCreateOrUpdateParameters()
            {
                Content = reader.ReadToEnd()
            };

            var editedPageResponse = await wikiClient.UpdatePageByIdAsync(
                parameters: parameters,
                project: wiki.ProjectId,
                wikiIdentifier: wiki.Name,
                id: pageId,
                Version: originalVersion).ConfigureAwait(false);

            var updatedContent = editedPageResponse.Page.Content;
            var updatedVersion = editedPageResponse.ETag.ToList()[0];

            Console.WriteLine("Before editing --> Page path: {0}, version: {1}, content: {2}", somePage.Path, originalVersion, originalContent);
            Console.WriteLine("After editing --> Page path: {0}, version: {1}, content: {2}", somePage.Path, updatedVersion, updatedContent);

            return editedPageResponse;
        }

        public static Task<WikiPageResponse> GetWikiPageByIdAndSubPages(VssConnection connection, Guid projectId, string wikiName)
        {
            var wikiClient = connection.GetClient<WikiHttpClient>();

            var wiki = Helpers.FindOrCreateProjectWiki(connection, projectId, wikiName);
            int somePageId = Helpers.GetAnyWikiPageId(connection, wiki);

            return wikiClient.GetPageByIdAsync(
                project: wiki.ProjectId,
                wikiIdentifier: wiki.Id,
                id: somePageId,
                recursionLevel: VersionControlRecursionType.OneLevel);
        }
    }
}
