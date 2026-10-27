using Backend.Application.Common;
using Backend.Application.Properties.Dtos;

namespace Backend.Application.Properties;

/// <summary>
/// Gestão de imóveis na área administrativa: cadastro, edição, exclusão, status e patrocínio.
/// </summary>
public interface IPropertyService
{
    Task<Result<PropertyResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<PropertyResponse>> CreateAsync(CreatePropertyRequest request, CancellationToken cancellationToken = default);

    Task<Result<PropertyResponse>> UpdateAsync(Guid id, UpdatePropertyRequest request, CancellationToken cancellationToken = default);

    Task<Result<PropertyResponse>> ChangeStatusAsync(Guid id, ChangePropertyStatusRequest request, CancellationToken cancellationToken = default);

    Task<Result<PropertyResponse>> SponsorAsync(Guid id, SponsorPropertyRequest request, CancellationToken cancellationToken = default);

    Task<Result<PropertyResponse>> RemoveSponsorshipAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
