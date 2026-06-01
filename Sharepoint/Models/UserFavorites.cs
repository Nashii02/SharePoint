using Sharepoint.Models;

public class UserFavorite
{
    public int Id { get; set; }
    public int AppUserId { get; set; }          // FK
    public AppUser AppUser { get; set; } = null!; // navigation
    public int WorkCategoryId { get; set; }       // FK
    public WorkCategory WorkCategory { get; set; } = null!; // navigation
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}