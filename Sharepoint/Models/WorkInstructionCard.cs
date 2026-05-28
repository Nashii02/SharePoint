namespace Sharepoint.Models
{
    public class WorkInstructionCard
    {
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string Url { get; set; } = "";
        public string IconClass { get; set; } = "";   // e.g. "ti ti-devices"
        public string Status { get; set; } = "";       // e.g. "Updated", "New doc", "Needs review"
        public string StatusColor { get; set; } = "";  // e.g. "updated", "new", "review"
        public string LastUpdated { get; set; } = "";  // e.g. "Apr 28, 2026"
    }
}