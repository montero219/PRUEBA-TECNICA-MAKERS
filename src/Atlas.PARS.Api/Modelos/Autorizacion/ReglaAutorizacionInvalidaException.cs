namespace Atlas.PARS.Api.Modelos.Autorizacion;

public sealed class ReglaAutorizacionInvalidaException : Exception
{
    public ReglaAutorizacionInvalidaException(string mensaje)
        : base(mensaje)
    {
    }
}
