using Logistics.Application.Common.Interfaces;
using Logistics.Application.Common.Models;
using Logistics.Domain.Services;
using MediatR;

namespace Logistics.Application.Shipments.Queries.AssessShipmentRisk;

/// <summary>Assess the delivery risk of an existing shipment.</summary>
public sealed record AssessShipmentRiskQuery(string ShipmentId) : IRequest<Result<RiskAssessmentDto>>;

public sealed record RiskFactorDto(string Name, double Points, string Reason);

public sealed record RiskAssessmentDto(
    string ShipmentId,
    double Score,
    string Band,
    IReadOnlyList<RiskFactorDto> Factors)
{
    public static RiskAssessmentDto FromDomain(RiskAssessment a) => new(
        a.ShipmentId,
        a.Score,
        a.Band.ToString(),
        a.Factors.Select(f => new RiskFactorDto(f.Name, f.Points, f.Reason)).ToList());
}

/// <summary>Use-case handler: load the aggregate, delegate scoring to the domain service, shape the result.</summary>
public sealed class AssessShipmentRiskQueryHandler(
    IShipmentRepository shipments,
    IShipmentRiskService riskService) : IRequestHandler<AssessShipmentRiskQuery, Result<RiskAssessmentDto>>
{
    public async Task<Result<RiskAssessmentDto>> Handle(AssessShipmentRiskQuery request, CancellationToken ct)
    {
        var shipment = await shipments.GetByIdAsync(request.ShipmentId, ct);
        if (shipment is null)
            return Result<RiskAssessmentDto>.Failure($"Shipment '{request.ShipmentId}' was not found.");

        var assessment = await riskService.AssessAsync(shipment, ct);
        return assessment is null
            ? Result<RiskAssessmentDto>.Failure("No route exists for this shipment; cannot assess risk.")
            : Result<RiskAssessmentDto>.Success(RiskAssessmentDto.FromDomain(assessment));
    }
}
