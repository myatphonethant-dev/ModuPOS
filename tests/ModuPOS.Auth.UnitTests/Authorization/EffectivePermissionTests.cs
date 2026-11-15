using ModuPOS.Auth.Authorization;

namespace ModuPOS.Auth.UnitTests.Authorization;

public class EffectivePermissionTests
{
    private static readonly Guid BranchA = Guid.NewGuid();
    private static readonly Guid BranchB = Guid.NewGuid();
    private static readonly Guid Manager = Guid.NewGuid();
    private static readonly Guid Cashier = Guid.NewGuid();
    private static readonly Guid Owner = Guid.NewGuid();

    private static readonly CatalogPermission[] Catalog =
    [
        new("role.view", "POS.AUTH"),
        new("role.assign", "POS.AUTH"),
        new("sale.create", "POS.SALE"),
        new("sale.refund", "POS.SALE")
    ];

    private static readonly Dictionary<Guid, IReadOnlyCollection<string>> RolePermissions = new()
    {
        [Manager] = ["role.view", "role.assign", "sale.refund"],
        [Cashier] = ["sale.create"],
        [Owner] = []
    };

    private static IReadOnlySet<string> Compute(Guid? branch, Func<string, bool>? feature = null, params AssignedRole[] roles) =>
        EffectivePermissionCalculator.Compute(roles, branch, RolePermissions, Catalog, feature ?? (_ => true));

    [Fact]
    public void Business_scope_role_applies_in_every_branch_and_without_a_branch()
    {
        var roles = new[] { new AssignedRole(Cashier, false, true, null) };

        Assert.Equal(["sale.create"], Compute(BranchA, null, roles));
        Assert.Equal(["sale.create"], Compute(BranchB, null, roles));
        Assert.Equal(["sale.create"], Compute(null, null, roles));
    }

    [Fact]
    public void Branch_scope_role_applies_only_in_that_branch()
    {
        var roles = new[] { new AssignedRole(Manager, false, true, BranchA) };

        Assert.Contains("role.assign", Compute(BranchA, null, roles));
        Assert.Empty(Compute(BranchB, null, roles));
        Assert.Empty(Compute(null, null, roles));
    }

    [Fact]
    public void Business_and_branch_roles_are_combined()
    {
        var roles = new[]
        {
            new AssignedRole(Cashier, false, true, null),
            new AssignedRole(Manager, false, true, BranchA)
        };

        Assert.Equal(["role.assign", "role.view", "sale.create", "sale.refund"], Compute(BranchA, null, roles).Order());
        Assert.Equal(["sale.create"], Compute(BranchB, null, roles));
    }

    [Fact]
    public void Inactive_role_grants_nothing()
    {
        var roles = new[] { new AssignedRole(Manager, false, IsActive: false, null) };

        Assert.Empty(Compute(BranchA, null, roles));
    }

    [Fact]
    public void Owner_gets_every_catalog_permission()
    {
        var roles = new[] { new AssignedRole(Owner, IsOwner: true, true, null) };

        Assert.Equal(Catalog.Select(p => p.Code).Order(), Compute(null, null, roles).Order());
    }

    [Fact]
    public void Permissions_of_disabled_features_are_dropped_even_for_owner()
    {
        Func<string, bool> noSales = f => f != "POS.SALE";

        Assert.Equal(["role.assign", "role.view"], Compute(null, noSales, new AssignedRole(Owner, true, true, null)).Order());
        Assert.Equal(["role.assign", "role.view"], Compute(BranchA, noSales, new AssignedRole(Manager, false, true, BranchA)).Order());
    }

    [Fact]
    public void Codes_missing_from_the_catalog_are_ignored()
    {
        var withRetired = new Dictionary<Guid, IReadOnlyCollection<string>> { [Cashier] = ["sale.create", "retired.code"] };

        var result = EffectivePermissionCalculator.Compute(
            [new AssignedRole(Cashier, false, true, null)], null, withRetired, Catalog, _ => true);

        Assert.Equal(["sale.create"], result);
    }
}
