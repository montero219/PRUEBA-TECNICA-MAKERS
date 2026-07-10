using Atlas.PARS.Api.Modelos.Autorizacion;

namespace Atlas.PARS.Api.Servicios.Interfaces;

public interface IFirmadorDecisionesAutorizacion
{
    FirmaDecision Firmar(PayloadFirmaDecision payload);

    bool Verificar(PayloadFirmaDecision payload, FirmaDecision firma);
}
