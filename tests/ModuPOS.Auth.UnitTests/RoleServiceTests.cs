namespace ModuPOS.Auth.UnitTests;

using Microsoft.EntityFrameworkCore;
using ModuPOS.Auth.DTOs;
using ModuPOS.Auth.Services;
using ModuPOS.Database.AppDbContextModels;

public class RoleServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(AppDbContext Context, Role Role, Permission View, Permission Create, Permission Delete)> SeedRoleWithPermissionsAsync()
    {
        var context = CreateContext();

        var view = new Permission { Id = 1, Name = "roles.view", IsDeleted = false };
        var create = new Permission { Id = 2, Name = "roles.create", IsDeleted = false };
        var delete = new Permission { Id = 3, Name = "roles.delete", IsDeleted = false };
        context.Permissions.AddRange(view, create, delete);

        var role = new Role { Id = 10, Name = "Cashier", IsDeleted = false };
        context.Roles.Add(role);

        context.RolePermissions.Add(new RolePermission
        {
            Id = 100,
            RoleId = role.Id,
            PermissionId = view.Id,
            IsDeleted = false
        });
        context.RolePermissions.Add(new RolePermission
        {
            Id = 101,
            RoleId = role.Id,
            PermissionId = create.Id,
            IsDeleted = true
        });

        await context.SaveChangesAsync();
        return (context, role, view, create, delete);
    }

    [Fact]
    public async Task UpdateRoleAsync_AddsAndRemovesPermissions_ReturnsFullEffectiveSet()
    {
        // Arrange
        var (context, role, view, _, delete) = await SeedRoleWithPermissionsAsync();
        await using (context)
        {
            var service = new RoleService(context);

            // Act
            var result = await service.UpdateRoleAsync(
                role.Id,
                new UpdateRoleRequest(
                    PermissionIdsToAdd: [delete.Id],
                    PermissionIdsToRemove: [view.Id]));

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("Cashier", result.Data.Name);
            Assert.Equal([delete.Id], result.Data.PermissionIds);
            Assert.Equal(["roles.delete"], result.Data.PermissionNames);

            var activeLinks = await context.RolePermissions
                .Where(rp => rp.RoleId == role.Id && !rp.IsDeleted)
                .Select(rp => rp.PermissionId)
                .ToListAsync();
            Assert.Equal([delete.Id], activeLinks);
        }
    }

    [Fact]
    public async Task UpdateRoleAsync_ReAddSoftDeletedPermission_RevivesExistingLink()
    {
        // Arrange
        var (context, role, view, create, _) = await SeedRoleWithPermissionsAsync();
        await using (context)
        {
            var service = new RoleService(context);

            // Act
            var result = await service.UpdateRoleAsync(
                role.Id,
                new UpdateRoleRequest(PermissionIdsToAdd: [create.Id]));

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal([view.Id, create.Id], result.Data.PermissionIds.OrderBy(id => id).ToList());

            var createLinks = await context.RolePermissions
                .Where(rp => rp.RoleId == role.Id && rp.PermissionId == create.Id)
                .ToListAsync();
            Assert.Single(createLinks);
            Assert.False(createLinks[0].IsDeleted);
        }
    }

    [Fact]
    public async Task UpdateRoleAsync_NameOnly_LeavesPermissionsUnchanged()
    {
        // Arrange
        var (context, role, view, _, _) = await SeedRoleWithPermissionsAsync();
        await using (context)
        {
            var service = new RoleService(context);

            // Act
            var result = await service.UpdateRoleAsync(role.Id, new UpdateRoleRequest(Name: "Lead Cashier"));

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("Lead Cashier", result.Data.Name);
            Assert.Equal([view.Id], result.Data.PermissionIds);
        }
    }

    [Fact]
    public async Task UpdateRoleAsync_OverlappingAddAndRemove_ReturnsFailure()
    {
        // Arrange
        var (context, role, view, _, _) = await SeedRoleWithPermissionsAsync();
        await using (context)
        {
            var service = new RoleService(context);

            // Act
            var result = await service.UpdateRoleAsync(
                role.Id,
                new UpdateRoleRequest(
                    PermissionIdsToAdd: [view.Id],
                    PermissionIdsToRemove: [view.Id]));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Permission IDs cannot appear in both add and remove lists.", result.Message);
        }
    }

    [Fact]
    public async Task UpdateRoleAsync_UnknownPermissionToAdd_ReturnsFailure()
    {
        // Arrange
        var (context, role, _, _, _) = await SeedRoleWithPermissionsAsync();
        await using (context)
        {
            var service = new RoleService(context);

            // Act
            var result = await service.UpdateRoleAsync(
                role.Id,
                new UpdateRoleRequest(PermissionIdsToAdd: [999]));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("One or more permissions to add were not found.", result.Message);
        }
    }
}
