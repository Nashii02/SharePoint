namespace Sharepoint.Models
{
    public class WorkInstructionViewModel
    {
        public List<WorkInstructionCard> FeaturedCards { get; set; } = new();
        public List<WorkInstructionCard> SecondaryCards { get; set; } = new();

        public Dictionary<string, List<WorkInstructionCard>> GroupedCards { get; set; }
    }
}
