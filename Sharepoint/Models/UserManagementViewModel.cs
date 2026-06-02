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
    }
}