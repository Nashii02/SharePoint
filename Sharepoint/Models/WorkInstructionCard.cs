using System.Runtime.CompilerServices;

namespace Sharepoint.Models
{
    public class WorkInstructionCard
    {
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string IconClass { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusColor { get; set; } = "";
        public string LastUpdated { get; set; } = "";
        public string URL { get; set; } = "";
        public string Category { get; set; } = "";
        public string? CategoryColor { get; set; }
        public bool IsFeatured { get; set; }

    }

    public class WorkInstructionViewModel
    {
        public List<WorkInstructionCard> FeaturedCards { get; set; } = new();
        public Dictionary<string, List<WorkInstructionCard>> GroupedSecondaryCards { get; set; } = new();

        public int TotalModules { get; set; }
        public int TotalCategories { get; set; }
        public int RecentlyUpdatedCount { get; set; }
    }
}
