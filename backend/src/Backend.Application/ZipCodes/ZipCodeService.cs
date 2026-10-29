using Backend.Application.Common;
using Backend.Application.ZipCodes.Dtos;
using Backend.Domain.Common;
using Backend.Domain.Properties;
using Backend.Infrastructure.ZipCodes;

namespace Backend.Application.ZipCodes;

public sealed class ZipCodeService(IZipCodeClient zipCodeClient) : IZipCodeService
{
    public async Task<Result<ZipCodeResponse>> GetAddressAsync(string zipCode, CancellationToken cancellationToken = default)
    {
        string normalizedZipCode;
        try
        {
            normalizedZipCode = Address.NormalizeZipCode(zipCode);
        }
        catch (DomainException exception)
        {
            return ZipCodeErrors.Invalid(exception.Message);
        }

        ZipCodeAddress? address;
        try
        {
            address = await zipCodeClient.FindAsync(normalizedZipCode, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return ZipCodeErrors.Unavailable();
        }

        return address is null ? ZipCodeErrors.NotFound(normalizedZipCode) : address.ToResponse();
    }
}
