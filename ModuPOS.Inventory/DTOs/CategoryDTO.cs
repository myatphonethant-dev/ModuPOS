namespace ModuPOS.Inventory.DTOs;

public class CreateCategoryRequest
{
    public string CategoryCode { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;
}

public class UpdateCategoryRequest
{
    public string CategoryCode { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;
}

public class CategoryResponseDto
{
    public int CategoryId { get; set; }

    public string CategoryCode { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;
}