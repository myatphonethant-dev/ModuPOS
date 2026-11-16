namespace ModuPOS.Inventory.DTOs;

public record CreateCategoryRequest(string CategoryCode, string CategoryName);

public record UpdateCategoryRequest(string CategoryCode, string CategoryName);

public record CategoryResponseDto(int CategoryId, string CategoryCode, string CategoryName);
