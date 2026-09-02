namespace FashionStore.Domain.Enums
{
    using System.ComponentModel;

    public enum RoleEnums
    {
        [Description("Super administrator")]
        SuperAdmin,
        [Description("Business administrator")]
        BusinessAdmin,
        [Description("Customer")]
        User
    }

    public static class RoleConstants
    {
        public const string SuperAdmin = nameof(RoleEnums.SuperAdmin);
        public const string BusinessAdmin = nameof(RoleEnums.BusinessAdmin);
        public const string User = nameof(RoleEnums.User);
        public static readonly IReadOnlySet<string> AdminRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            SuperAdmin,
            BusinessAdmin
        };
    }
}
