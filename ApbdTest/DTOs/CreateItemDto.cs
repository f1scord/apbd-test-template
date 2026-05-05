using System.ComponentModel.DataAnnotations;

namespace ApbdTest.DTOs;

public class CreateItemDto
{
    // TODO: add item fields, add [StringLength] or [Required] where needed
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
