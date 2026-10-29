using Backend.Application.Common;
using Backend.Application.ZipCodes.Dtos;

namespace Backend.Application.ZipCodes;

public interface IZipCodeService
{
    Task<Result<ZipCodeResponse>> GetAddressAsync(string zipCode, CancellationToken cancellationToken = default);
}
