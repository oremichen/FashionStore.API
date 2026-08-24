
using Microsoft.AspNetCore.Identity;

namespace FashionStore.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        // ── Personal Info ──────────────────────────────────────────
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? AvatarUrl { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Gender { get; set; }

        // ── Account Lifecycle ──────────────────────────────────────
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public DateTimeOffset? LastLoginDate { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }          // soft delete
        public string UserStatus { get; set; }
        public bool IsDeactivated { get; set; }
        public bool IsDeleted { get; set; }

        // ── Auth & Security ────────────────────────────────────────
        public string? RefreshToken { get; set; }
        public DateTimeOffset RefreshTokenExpiryTime { get; set; }
        public bool? IsPasswordChanged { get; set; }
        public DateTimeOffset? PasswordChangedAt { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTimeOffset? LockedOutAt { get; set; }

        // ── Onboarding & Compliance ────────────────────────────────
        public bool? AgreeToPolicy { get; set; }
        public DateTimeOffset? PolicyAgreedAt { get; set; }
        public bool EmailVerified { get; set; }
        public DateTimeOffset? InviteResendDateTime { get; set; }

        // ── Loyalty & Preferences ──────────────────────────────────
        public int LoyaltyPoints { get; set; }
        public string? PreferredCurrency { get; set; }          // e.g. "USD"
        public string? PreferredLanguage { get; set; }          // e.g. "en-US"
        public bool MarketingEmailsEnabled { get; set; } = true;
        public bool SmsNotificationsEnabled { get; set; }

        // ── Navigation Properties ──────────────────────────────────
       // public List<UserLog> UserLogs { get; set; } = [];
        public ICollection<Address> Addresses { get; set; } = [];
      //  public ICollection<Order> Orders { get; set; } = [];
       // public ICollection<Wishlist> Wishlist { get; set; } = [];
     //   public ICollection<Cart> Cart { get; set; } = [];
    }
}
