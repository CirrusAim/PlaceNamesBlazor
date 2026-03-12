namespace PlaceNamesBlazor.Services.StampType;

public interface IStampTypeService
{
    Task<(bool Success, int? StempeltypeId, string? Error)> CreateAsync(string hovedstempeltype, string stempeltypeFullTekst, CancellationToken cancellationToken = default);
}
