namespace Atlas.PARS.Api.Modelos.Autorizacion;

public sealed record PayloadFirmaDecision(
    Guid IdDecision,
    string CodigoOrganizacion,
    string Decision,
    string? CodigoRegla,
    string SolicitudHash,
    string CorrelationId,
    DateTimeOffset FechaDecision);

public sealed record FirmaDecision(
    string KeyId,
    string Algoritmo,
    string Valor);
