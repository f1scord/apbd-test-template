using ApbdTest.DTOs;

namespace ApbdTest.Services;

public interface IDbService
{
    // TODO: rename methods and params to match exam
    Task<GetRootDto> GetAsync(int id);
    Task CreateAsync(int id, CreateRootDto dto);
}
