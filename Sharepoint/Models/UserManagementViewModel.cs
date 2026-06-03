namespace Sharepoint.Models
{
    public class UserManagementViewModel
    {
        public List<UserRow> Users { get; set; } = new();
        public List<string> AllRoles { get; set; } = new();
    }

    public class UserRow
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Nickname { get; set; }
        public string DisplayName { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsGuest { get; set; }
        public bool IsBanned { get; set; }
        public bool IsVerified { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
    }
}