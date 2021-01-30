namespace ReleaseNotes
{
    internal record WorkItemRecord (string Title, int? Id, string Url, WorkItemType WorkItemType);

    internal enum WorkItemType : byte
    {
        Us = 0,
        Bug = 1,
        Feature = 2,
        Epic = 3
    }
}
