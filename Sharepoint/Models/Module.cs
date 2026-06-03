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
        public string? CategoryColor { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int ViewCount { get; set; } = 0;
    }

    public class ModuleFile
    {
        public int Id { get; set; }
        public string ModuleSlug { get; set; } = "";
        public string FileName { get; set; } = "";
        public string FileType { get; set; } = "";
        public string? FilePath { get; set; }
        public string FileUrl { get; set; } = "";
        public string? FileSize { get; set; }
        public DateTime UploadedDate { get; set; }
        public string? Description { get; set; }
        public string Url => FileUrl;
        public int Version { get; set; } = 1;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
        public string? OriginalFileName { get; set; }
        public string UploadedDateFormatted => UploadedDate.ToString("MMM dd, yyyy");
    }

    public class ModulePageViewModel
    {
        public string ModuleTitle { get; set; } = "";
        public string ModuleSubtitle { get; set; } = "";
        public string ModuleSlug { get; set; } = "";
        public List<ModuleFile> Files { get; set; } = new();
        public List<ModuleFile> DeletedFiles { get; set; } = new();

        // ... existing properties
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public bool IsLikedByMe { get; set; }
        public List<ModuleComment> Comments { get; set; } = new();
        

    }

    public class ModuleReaction
    {
        public int Id { get; set; }
        public string ModuleSlug { get; set; } = "";
        public string UserId { get; set; } = "";  // identity user id
        public DateTime CreatedAt { get; set; }
    }

    public class ModuleComment
    {
        public int Id { get; set; }
        public string ModuleSlug { get; set; } = "";
        public string UserId { get; set; } = "";
        public string UserEmail { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? EditedAt { get; set; }
        public bool IsEdited => EditedAt.HasValue;
    }

    public class GuestUser
    {
        public int Id { get; set; }
        public string Nickname { get; set; } = "";
        public string SessionId { get; set; } = "";  // Track guests across session
        public DateTime CreatedAt { get; set; }
        public bool IsBanned { get; set; } = false;
        public DateTime? BannedAt { get; set; }
        public string? BannedReason { get; set; }
    }
}
