using Microsoft.EntityFrameworkCore;
using ModuPOS.Auth.Accounts;
using ModuPOS.Auth.Roles;
using ModuPOS.Database;
using ModuPOS.Organization.Entities;

namespace ModuPOS.Api.Infrastructure;

/// <summary>
/// Development data: business "Demo", branches A and B (active) and C (inactive), an Owner and a
/// staff account assigned to A and B. Logins and the dev-only password come from the DevSeed config.
/// </summary>
public static class DevSeeder
{
    public static async Task MigrateAndSeedAsync(WebApplication app)
    {
        var config = app.Configuration;
        if (!config.GetValue<bool>("Database:MigrateOnStartup"))
            return;

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ModuPOSDbContext>();
        await db.Database.MigrateAsync();

        if (!app.Environment.IsDevelopment() || !config.GetValue<bool>("DevSeed:Enabled"))
            return;

        if (await db.Set<Business>().AnyAsync(b => b.Name == "Demo"))
            return;

        // Default roles reference catalog permissions, which must exist first.
        await app.Services.GetRequiredService<ModuPOS.Auth.Permissions.PermissionCatalogSync>().SyncAsync(CancellationToken.None);

        var password = config["DevSeed:Password"]
            ?? throw new InvalidOperationException("DevSeed:Password is not configured.");
        var passwords = scope.ServiceProvider.GetRequiredService<PasswordService>();

        var business = new Business { Name = "Demo" };
        db.Add(business);
        var branchA = new Branch { Name = "Branch A", BusinessId = business.Id };
        var branchB = new Branch { Name = "Branch B", BusinessId = business.Id };
        var branchC = new Branch { Name = "Branch C", BusinessId = business.Id, Status = BranchStatus.Inactive };
        db.AddRange(branchA, branchB, branchC);

        var owner = Account(config["DevSeed:OwnerLogin"] ?? "owner", password, passwords);
        var staff = Account(config["DevSeed:StaffLogin"] ?? "staff", password, passwords);
        db.AddRange(owner, staff);
        await db.SaveChangesAsync();

        var ownerProfile = new Profile { UserAccountId = owner.Id, Type = ProfileType.Staff, BusinessId = business.Id };
        var staffProfile = new Profile { UserAccountId = staff.Id, Type = ProfileType.Staff, BusinessId = business.Id };
        db.AddRange(ownerProfile, staffProfile);

        await db.SaveChangesAsync();

        // Owner, Manager and Cashier; the owner account gets the Owner role.
        await scope.ServiceProvider.GetRequiredService<DefaultRolesProvisioner>()
            .ProvisionAsync(business.Id, ownerProfile.Id, CancellationToken.None);

        db.AddRange(
            new StaffBranchAssignment { BusinessId = business.Id, StaffProfileId = ownerProfile.Id, BranchId = branchA.Id },
            new StaffBranchAssignment { BusinessId = business.Id, StaffProfileId = ownerProfile.Id, BranchId = branchB.Id },
            new StaffBranchAssignment { BusinessId = business.Id, StaffProfileId = staffProfile.Id, BranchId = branchA.Id },
            new StaffBranchAssignment { BusinessId = business.Id, StaffProfileId = staffProfile.Id, BranchId = branchB.Id });
        await db.SaveChangesAsync();

        app.Logger.LogInformation("Seeded development business 'Demo' with users '{Owner}' and '{Staff}'.", owner.Username, staff.Username);
    }

    private static UserAccount Account(string login, string password, PasswordService passwords)
    {
        var account = new UserAccount { Username = login, Email = $"{login}@demo.test" };
        account.PasswordHash = passwords.Hash(account, password);
        return account;
    }
}
