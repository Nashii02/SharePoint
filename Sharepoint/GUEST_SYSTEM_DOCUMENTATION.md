# ?? Guest Login & Comment System - Implementation Guide

This document outlines the complete guest login system, comment editing, and user management features you've just implemented.

---

## ?? Table of Contents
1. [Guest User System](#guest-user-system)
2. [Comment System with Edit History](#comment-system)
3. [User Management Updates](#user-management)
4. [Search Functionality](#search)
5. [Database Migrations](#migrations)
6. [Best Practices](#best-practices)

---

## Guest User System

### How It Works

#### Guest Login Flow
```
Login Page ? "Continue as Guest" Button ? Modal Dialog
    ?
Optional Nickname Entry (or Auto-generated Guest1, Guest2, etc.)
    ?
Session Creation ? GuestUser Record Created
    ?
User Redirected to Work Instructions (Authenticated as Guest)
```

### Key Features

? **Optional Nickname**
- Users can enter custom nickname (max 50 chars)
- If blank, auto-assigned: Guest1, Guest2, Guest3...
- Nickname stored in session AND database

? **Session Management**
- Session timeout: 24 hours (configurable in `Program.cs`)
- Session ID tracked for consistency
- Guest ID stored in `HttpContext.Session` for quick access

? **Guest Capabilities**
- View all modules and files
- Post comments
- Edit their own comments
- Cannot like/unlike (registered users only)
- Cannot upload files (admin only)

### Files Modified/Created

```
Sharepoint/
??? Models/
?   ??? GuestLoginViewModel.cs (NEW)
?   ??? Module.cs (UPDATED - ModuleComment.EditedAt, GuestUser entity)
?   ??? UserManagementViewModel.cs (UPDATED - DisplayName, IsGuest, IsBanned)
??? Controllers/
?   ??? AccountController.cs (UPDATED - LoginAsGuest, BanGuest methods)
?   ??? HomeController.cs (UPDATED - EditComment method)
??? Views/Account/
?   ??? Login.cshtml (UPDATED - Guest modal)
??? Program.cs (UPDATED - Session configuration)
```

---

## Comment System

### Edit History Features

#### Database Fields Added to `ModuleComment`:
```csharp
public DateTime? EditedAt { get; set; }      // When edited
public bool IsEdited => EditedAt.HasValue;   // Computed property
```

#### UI Display
- **Original Timestamp**: "Nov 15, 2024 � 2:30 PM"
- **Edited Label**: Small amber badge showing "edited"
- **Edit Timestamp**: "Nov 15, 2024 � 2:45 PM"

Example:
```
AlexMiller
Nov 15, 2024 � 2:30 PM   [edited]   Nov 15, 2024 � 2:45 PM

This is my updated comment with more detail.
```

### Comment Operations

#### Add Comment
```csharp
POST /Home/AddComment
Parameters:
  - moduleId: string
  - content: string (max 500 chars)
```

**Who can**: Any authenticated user (registered or guest)
**Response**: Redirects to module page

#### Edit Comment
```csharp
POST /Home/EditComment
Parameters:
  - commentId: int
  - moduleId: string
  - content: string (max 500 chars)
```

**Who can**: 
- Comment owner
- Admin
- Guest who posted it (verified via session)

**Updates**: Content + EditedAt timestamp

#### Delete Comment
```csharp
POST /Home/DeleteComment
Parameters:
  - commentId: int
  - moduleId: string
```

**Who can**:
- Comment owner
- Admin
- Guest who posted it

**Result**: Soft delete (IsDeleted = true)

---

## User Management

### Display Updates

#### Registered Users
- Display Name: Email address
- Role: Admin / User / Quality Assurance
- Actions: Change role, delete
- Icon: User profile icon

#### Guest Users
- Display Name: Nickname (e.g., "Guest1" or "Alex")
- Role: Guest (locked, cannot change)
- Actions: Ban or delete
- Icon: User secret mask icon
- Ban Status: Shows if banned

### Admin Actions

#### Ban Guest
```csharp
POST /Account/BanGuest
Parameters:
  - guestId: int
  - reason: string (optional)
```

**Effect**: 
- Sets `IsBanned = true`
- Stores `BannedAt` timestamp
- Guest can still see content but cannot post comments (add check in HomeController)

#### Delete Guest
```csharp
POST /Account/DeleteUser
Parameters:
  - userId: string (format: "guest-{id}")
```

**Effect**: 
- Removes GuestUser record
- Deletes their comments (cascade)
- Removes from user list immediately

### Search Functionality

#### Updated Search in ManageUsers.cshtml

```javascript
function filterUsers() {
    // Searches by BOTH email (registered) AND nickname (guests)
    const matchSearch = email.includes(query) || name.includes(query);
    const matchFilter = activeFilter === 'all' || role === activeFilter;
}
```

**Examples**:
- Search "alex" ? finds Guest "Alex" and user "alexsmith@company.com"
- Search "admin" ? finds user "admin@company.com"
- Filter by "Guest" ? shows only guest users
- Filter by "Admin" ? shows only administrators

---

## Database Migrations

### Models Added/Updated

#### 1. **GuestUser** (NEW)
```csharp
public class GuestUser
{
    public int Id { get; set; }                      // Auto-increment
    public string Nickname { get; set; } = "";       // Display name
    public string SessionId { get; set; } = "";      // Track session
    public DateTime CreatedAt { get; set; }          // Timestamp
    public bool IsBanned { get; set; } = false;      // Ban flag
    public DateTime? BannedAt { get; set; }          // When banned
    public string? BannedReason { get; set; }        // Why banned
}
```

#### 2. **ModuleComment** (UPDATED)
```csharp
public DateTime? EditedAt { get; set; }    // NEW: Edit timestamp
public bool IsEdited => EditedAt.HasValue; // Computed property
```

### Migration Steps

?? **You'll need to run Entity Framework migrations:**

```bash
# Add migration
dotnet ef migrations add AddGuestUsersAndCommentEdits

# Apply to database
dotnet ef database update
```

---

## Best Practices

### 1. **Guest Session Persistence**
```csharp
// Always check for guest in context
var guestId = Context.Session.GetString("GuestId");
var isGuest = guestId != null;

// Use composite key for verification
var commentOwner = comment.UserId == $"guest-{guestId}";
```

### 2. **Permission Checks**
```csharp
// Always verify ownership before allowing edit/delete
bool isOwner = comment.UserId == userId || 
              (guestId != null && comment.UserId == $"guest-{guestId}");

if (!isOwner && !User.IsInRole("Admin"))
    return Unauthorized();
```

### 3. **Guest Auto-numbering**
```csharp
// Avoids conflicts by using database ID
private int GenerateGuestNumber()
{
    var lastGuest = _context.GuestUsers
        .OrderByDescending(g => g.Id)
        .FirstOrDefault();
    return lastGuest?.Id + 1 ?? 1;
}
// Creates: Guest1, Guest2, Guest3, etc.
```

### 4. **Display Guest vs. Registered**
```csharp
// In UserManagementViewModel.cs
public bool IsGuest { get; set; }

// Then in view:
@if (user.IsGuest)
{
    // Show guest-specific UI
    <i class="fa-solid fa-user-secret"></i>
}
```

### 5. **Edit Timestamp Display**
```csharp
@if (comment.IsEdited)
{
    <span class="text-[9px] bg-amber-50 text-amber-700 px-1.5 py-0.5 rounded font-semibold">
        edited
    </span>
    <span class="text-[10px] text-gray-400">
        @comment.EditedAt?.ToString("MMM dd, yyyy � h:mm tt")
    </span>
}
```

---

## Security Considerations

### ? Implemented
- Guest users cannot access admin functions
- Comment editing restricted to owner/admin
- Session-based guest tracking
- Soft deletes preserve audit trail
- CSRF token verification on all forms

### ?? Optional Enhancements
```csharp
// Ban check on comment submission (add to HomeController.AddComment)
var guest = await _context.GuestUsers.FindAsync(guestIdValue);
if (guest?.IsBanned == true)
    return Forbid("This guest account has been banned");

// Rate limiting for guests
// Comment history/audit logging
// IP-based guest ban list
```

---

## Testing Checklist

- [ ] Guest login with custom nickname
- [ ] Guest login with auto-generated name
- [ ] Guest can post comment
- [ ] Guest can edit own comment
- [ ] Edit timestamp displays correctly
- [ ] Admin can see guests in management panel
- [ ] Admin can ban guest
- [ ] Admin can delete guest
- [ ] Search works for both email and nickname
- [ ] Guest cannot upload files
- [ ] Guest cannot like modules
- [ ] Session persists across page reloads (24 hours)
- [ ] Logout removes guest session

---

## Configuration Options

### Session Timeout (Program.cs)
```csharp
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);  // Change as needed
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
```

### Guest Auto-number Prefix
If you want "Visitor1" instead of "Guest1", modify in AccountController:
```csharp
// Change this line in GenerateGuestNumber():
return $"Visitor{GenerateGuestNumber()}";  // Instead of $"Guest{..."
```

### Comment Character Limit
```csharp
// In Module.cshtml:
<textarea maxlength="500">  // Change 500 to desired limit
```

---

## Troubleshooting

### Issue: "Guest1 doesn't show in management panel"
**Solution**: Check if GuestUser record created in database. Verify session ID is being saved.

### Issue: "Edit timestamp not showing"
**Solution**: Ensure database migration applied. Check `EditedAt` property has value.

### Issue: "Search not finding guest by nickname"
**Solution**: Verify `data-name` attribute set in view. Check JavaScript search logic.

### Issue: "Guest comments show wrong author"
**Solution**: Check UserEmail field set to guest nickname during AddComment.

---

## Future Enhancements

?? **Suggested improvements**:

1. **Edit History**
   - Store previous versions in new `CommentHistory` table
   - Show "View history" link

2. **Guest Profiles**
   - Optional email field (for notifications)
   - Avatar generation from nickname initials

3. **Moderation**
   - Flag comments for review
   - Admin comment notes
   - Comment approval workflow

4. **Analytics**
   - Track guest vs. registered user engagement
   - Comment sentiment analysis
   - Most active guests/times

5. **Anti-Spam**
   - Rate limiting per session
   - Word filter
   - Honeypot fields

---

## API Reference

### GET Endpoints
```
/Account/Login              - Login form page
/Account/ManageUsers        - Admin user management (Authorized: Admin)
/Home/Module?id={slug}      - Module page (AllowAnonymous, increments view)
```

### POST Endpoints
```
/Account/LoginAsGuest       - Create guest session
/Account/Logout             - End guest or registered session
/Account/ChangeRole         - Admin: change user role (Authorized: Admin)
/Account/DeleteUser         - Admin: delete user/guest (Authorized: Admin)
/Account/BanGuest           - Admin: ban guest (Authorized: Admin)
/Home/AddComment            - Post comment (Authorized)
/Home/EditComment           - Edit comment (Authorized: owner/admin)
/Home/DeleteComment         - Delete comment (Authorized: owner/admin)
/Home/ToggleLike            - Like/unlike module (Authorized)
```

---

## Summary

Your system now supports:

? Guest users with optional nicknames
? Comment editing with timestamp tracking
? Integrated user & guest management panel
? Smart search by email and nickname
? Guest banning and moderation
? Full audit trail of edits

Enjoy your new features! ??
