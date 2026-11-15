using ModuPOS.Auth.Roles;

namespace ModuPOS.Auth.UnitTests.Roles;

public class RoleValidationTests
{
    private static readonly Dictionary<string, PermissionInfo> Catalog = new(StringComparer.Ordinal)
    {
        ["sale.view"] = new("sale.view", [], PermissionScope.Business, IsRetired: false, Available: true),
        ["sale.refund"] = new("sale.refund", ["sale.view"], PermissionScope.Business, false, true),
        ["sale.void"] = new("sale.void", ["sale.refund"], PermissionScope.Business, false, true),
        ["stock.adjust"] = new("stock.adjust", [], PermissionScope.Business, false, Available: false),
        ["old.thing"] = new("old.thing", [], PermissionScope.Business, IsRetired: true, Available: true),
        ["platform.audit.view"] = new("platform.audit.view", [], PermissionScope.Platform, false, true)
    };

    private static readonly HashSet<string> Everything = [.. Catalog.Keys];

    private static RoleDraft Draft(params string[] permissions) => new("Senior Cashier", "Handles refunds", permissions);

    [Fact]
    public void Valid_role_passes_with_no_added_dependencies()
    {
        var result = RoleValidator.Validate(Draft("sale.view", "sale.refund"), Catalog, Everything);

        Assert.True(result.IsSuccess);
        Assert.Equal(["sale.refund", "sale.view"], result.Value!.Permissions.Order());
        Assert.Empty(result.Value.AddedDependencies);
    }

    [Fact]
    public void Missing_dependencies_are_added_transitively_and_reported()
    {
        var result = RoleValidator.Validate(Draft("sale.void"), Catalog, Everything);

        Assert.True(result.IsSuccess);
        Assert.Equal(["sale.refund", "sale.view", "sale.void"], result.Value!.Permissions.Order());
        Assert.Equal(["sale.refund", "sale.view"], result.Value.AddedDependencies.Order());
    }

    [Theory]
    [InlineData("nope.never")]
    [InlineData("old.thing")]
    [InlineData("platform.audit.view")]
    public void Unknown_retired_or_platform_permissions_are_rejected(string code)
    {
        var result = RoleValidator.Validate(Draft(code), Catalog, Everything);

        Assert.Equal("permission_unknown", result.Error!.Code);
        Assert.Equal(422, result.Error.Status);
    }

    [Fact]
    public void Permission_of_a_disabled_feature_is_unavailable()
    {
        var result = RoleValidator.Validate(Draft("stock.adjust"), Catalog, Everything);

        Assert.Equal("permission_unavailable", result.Error!.Code);
    }

    [Fact]
    public void Granting_more_than_the_actor_holds_is_blocked_including_added_dependencies()
    {
        // Actor holds sale.refund but not sale.view, which sale.refund pulls in.
        var result = RoleValidator.Validate(Draft("sale.refund"), Catalog, new HashSet<string> { "sale.refund" });

        Assert.Equal("escalation_blocked", result.Error!.Code);
        Assert.Equal(403, result.Error.Status);
        Assert.Equal(new[] { "sale.view" }, (IEnumerable<string>)result.Error.Extensions!["permissions"]!);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("  ")]
    public void Name_must_be_2_to_60_characters(string name)
    {
        var result = RoleValidator.Validate(new RoleDraft(name, null, ["sale.view"]), Catalog, Everything);
        Assert.Equal("role_name_invalid", result.Error!.Code);
    }

    [Fact]
    public void Name_of_61_characters_is_rejected_and_name_is_trimmed()
    {
        Assert.Equal("role_name_invalid", RoleValidator.Validate(new RoleDraft(new string('x', 61), null, ["sale.view"]), Catalog, Everything).Error!.Code);
        Assert.Equal("Ok", RoleValidator.Validate(new RoleDraft("  Ok  ", null, ["sale.view"]), Catalog, Everything).Value!.Name);
    }

    [Fact]
    public void Description_over_500_characters_is_rejected()
    {
        var result = RoleValidator.Validate(new RoleDraft("Valid", new string('d', 501), ["sale.view"]), Catalog, Everything);
        Assert.Equal("role_description_invalid", result.Error!.Code);
    }

    [Fact]
    public void At_least_one_permission_is_required()
    {
        var result = RoleValidator.Validate(Draft(), Catalog, Everything);
        Assert.Equal("permissions_required", result.Error!.Code);
    }
}
