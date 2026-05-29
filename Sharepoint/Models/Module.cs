namespace Sharepoint.Models
{
    public class Module
    {
        public int Id { get; set; }
        public string Slug { get; set; } = "";
        public string Title { get; set; } = "";
        public string? Subtitle { get; set; }
        public string? IconClass { get; set; }
        public bool IsFeatured { get; set; }
        public string? Status { get; set; }
        public string? StatusColor { get; set; }
        public string? LastUpdated { get; set; }
        public DateTime? LastFileUpload { get; set; }
        public string? Category { get; set; }
    }

    public class ModuleFile
    {
        public int Id { get; set; }
        public string ModuleSlug { get; set; } = "";
        public string FileName { get; set; } = "";
        public string FileType { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string FileUrl { get; set; } = "";
        public string UploadedBy { get; set; } = "";
        public DateTime UploadedDate { get; set; }
        public string? Description { get; set; } = "";
        public string? FileSize { get; set; } = "";
        public string Url => FileUrl;
        public int Version { get; set; } = 1;
        public string? OriginalFileName { get; set; }
        public string UploadedDateFormatted => UploadedDate.ToString("MMM dd, yyyy");
    }

    public class ModulePageViewModel
    {
        public string ModuleTitle { get; set; } = "";
        public string ModuleSubtitle { get; set; } = "";
        public List<ModuleFile> Files { get; set; } = new();
        public string ModuleSlug { get; set; } = "";
    }
}