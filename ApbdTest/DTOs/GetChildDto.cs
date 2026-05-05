namespace ApbdTest.DTOs;

public class GetChildDto
{
    // TODO: add child-level fields
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public DateTime? NullableDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<GetLeafDto> Items { get; set; } = [];
}
