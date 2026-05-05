namespace ApbdTest.DTOs;

public class CreateRootDto
{
    // TODO: add POST body fields
    public DateTime Date { get; set; }
    public List<CreateItemDto> Items { get; set; } = [];
}
