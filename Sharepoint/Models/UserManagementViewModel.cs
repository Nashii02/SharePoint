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
        public string Role { get; set; } = "";
        public string DisplayName { get; set; } = "";  // Email for registered, Nickname for guests
        public bool IsGuest { get; set; } = false;
        public bool IsBanned { get; set; } = false;
    }
}