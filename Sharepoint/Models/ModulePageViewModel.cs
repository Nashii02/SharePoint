namespace Sharepoint.Models
{
    public class ModulePageViewModel
    {
        public string ModuleTitle { get; set; } = "";
        public string ModuleSubtitle { get; set; } = "";
        public List<ModuleFile> Files { get; set; } = new();
    }

    public class ModuleFile
    {
        public string FileName { get; set; } = "";
        public string FileType { get; set; } = ""; // pdf, docx, xlsx, etc.
        public string UploadedBy { get; set; } = "";
        public string UploadedDate { get; set; } = "";
        public string Url { get; set; } = "#";
    }
}