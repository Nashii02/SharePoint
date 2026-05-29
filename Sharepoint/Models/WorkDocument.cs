namespace Sharepoint.Models
{
        public class WorkDocument
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string Category { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
            public byte[] FileData { get; set; } = Array.Empty<byte>(); // PDF stored here
            public long FileSizeBytes { get; set; }
            public DateTime UploadedAt { get; set; } = DateTime.Now;
        }
}
