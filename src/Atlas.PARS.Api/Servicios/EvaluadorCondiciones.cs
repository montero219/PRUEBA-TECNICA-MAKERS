using System.Globalization;
using System.Text.Json;
using Atlas.PARS.Api.Modelos.Autorizacion;
using Atlas.PARS.Api.Servicios.Interfaces;

namespace Atlas.PARS.Api.Servicios;

public sealed class EvaluadorCondiciones : IEvaluadorCondiciones
{
    public bool Evaluar(
        JsonDocument condiciones,
        SolicitudAutorizacion solicitud)
    {
        var raiz = condiciones.RootElement;

        if (raiz.ValueKind != JsonValueKind.Object ||
            !raiz.TryGetProperty("todas", out var todas) ||
            todas.ValueKind != JsonValueKind.Array)
        {
            throw new ReglaAutorizacionInvalidaException(
                "La regla debe definir un arreglo de condiciones en 'todas'.");
        }

        foreach (var condicion in todas.EnumerateArray())
        {
            if (!EvaluarCondicion(condicion, solicitud))
            {
                return false;
            }
        }

        return true;
    }

    private static bool EvaluarCondicion(
        JsonElement condicion,
        SolicitudAutorizacion solicitud)
    {
        var fuente = ObtenerTextoRequerido(condicion, "fuente");
        var atributo = ObtenerTextoRequerido(condicion, "atributo");
        var operador = ObtenerTextoRequerido(condicion, "operador");

        if (!ResolverValor(solicitud, fuente, atributo, out var valorIzquierdo))
        {
            return false;
        }

        return operador switch
        {
            "igual" => EvaluarIgualdad(condicion, solicitud, valorIzquierdo),
            "distinto" => EvaluarDistinto(condicion, solicitud, valorIzquierdo),
            "mayor_o_igual" => EvaluarComparacionNumerica(
                condicion,
                solicitud,
                valorIzquierdo,
                (izquierda, derecha) => izquierda >= derecha),
            "menor_o_igual" => EvaluarComparacionNumerica(
                condicion,
                solicitud,
                valorIzquierdo,
                (izquierda, derecha) => izquierda <= derecha),
            "en" => EvaluarEn(condicion, valorIzquierdo),
            "entre_horas" => EvaluarEntreHoras(condicion, valorIzquierdo),
            _ => throw new ReglaAutorizacionInvalidaException(
                $"El operador '{operador}' no es soportado.")
        };
    }

    private static bool EvaluarIgualdad(
        JsonElement condicion,
        SolicitudAutorizacion solicitud,
        string valorIzquierdo)
    {
        if (condicion.TryGetProperty("compararCon", out var compararCon))
        {
            if (!ResolverValorComparado(solicitud, compararCon, out var valorDerecho))
            {
                return false;
            }

            return string.Equals(
                valorIzquierdo,
                valorDerecho,
                StringComparison.OrdinalIgnoreCase);
        }

        if (!condicion.TryGetProperty("valor", out var valor))
        {
            throw new ReglaAutorizacionInvalidaException(
                "La condicion debe definir 'valor' o 'compararCon'.");
        }

        return ValorCoincide(valorIzquierdo, valor);
    }

    private static bool EvaluarDistinto(
        JsonElement condicion,
        SolicitudAutorizacion solicitud,
        string valorIzquierdo)
    {
        if (condicion.TryGetProperty("compararCon", out var compararCon))
        {
            if (!ResolverValorComparado(solicitud, compararCon, out var valorDerecho))
            {
                return false;
            }

            return !string.Equals(
                valorIzquierdo,
                valorDerecho,
                StringComparison.OrdinalIgnoreCase);
        }

        if (!condicion.TryGetProperty("valor", out var valor))
        {
            throw new ReglaAutorizacionInvalidaException(
                "La condicion debe definir 'valor' o 'compararCon'.");
        }

        return !ValorCoincide(valorIzquierdo, valor);
    }

    private static bool EvaluarComparacionNumerica(
        JsonElement condicion,
        SolicitudAutorizacion solicitud,
        string valorIzquierdo,
        Func<decimal, decimal, bool> comparar)
    {
        if (!TryConvertirDecimal(valorIzquierdo, out var numeroIzquierdo))
        {
            return false;
        }

        if (condicion.TryGetProperty("compararCon", out var compararCon))
        {
            if (!ResolverValorComparado(solicitud, compararCon, out var valorDerecho) ||
                !TryConvertirDecimal(valorDerecho, out var numeroDerecho))
            {
                return false;
            }

            return comparar(numeroIzquierdo, numeroDerecho);
        }

        if (!condicion.TryGetProperty("valor", out var valor) ||
            !TryConvertirDecimal(valor, out var numeroValor))
        {
            throw new ReglaAutorizacionInvalidaException(
                "La comparacion numerica debe definir un valor numerico.");
        }

        return comparar(numeroIzquierdo, numeroValor);
    }

    private static bool EvaluarEn(
        JsonElement condicion,
        string valorIzquierdo)
    {
        if (!condicion.TryGetProperty("valor", out var valores) ||
            valores.ValueKind != JsonValueKind.Array)
        {
            throw new ReglaAutorizacionInvalidaException(
                "El operador 'en' requiere un arreglo en 'valor'.");
        }

        return valores.EnumerateArray()
            .Any(valor => ValorCoincide(valorIzquierdo, valor));
    }

    private static bool EvaluarEntreHoras(
        JsonElement condicion,
        string valorIzquierdo)
    {
        var desdeTexto = ObtenerTextoRequerido(condicion, "desde");
        var hastaTexto = ObtenerTextoRequerido(condicion, "hasta");

        if (!TryConvertirHora(valorIzquierdo, out var hora) ||
            !TryConvertirHora(desdeTexto, out var desde) ||
            !TryConvertirHora(hastaTexto, out var hasta))
        {
            return false;
        }

        if (desde <= hasta)
        {
            return hora >= desde && hora <= hasta;
        }

        return hora >= desde || hora <= hasta;
    }

    private static bool ResolverValorComparado(
        SolicitudAutorizacion solicitud,
        JsonElement compararCon,
        out string valor)
    {
        var fuente = ObtenerTextoRequerido(compararCon, "fuente");
        var atributo = ObtenerTextoRequerido(compararCon, "atributo");

        return ResolverValor(solicitud, fuente, atributo, out valor);
    }

    private static bool ResolverValor(
        SolicitudAutorizacion solicitud,
        string fuente,
        string atributo,
        out string valor)
    {
        var origen = fuente switch
        {
            "actor" => solicitud.AtributosActor,
            "recurso" => solicitud.AtributosRecurso,
            "contexto" => solicitud.Contexto,
            _ => throw new ReglaAutorizacionInvalidaException(
                $"La fuente '{fuente}' no es soportada.")
        };

        if (origen.TryGetValue(atributo, out var valorEncontrado))
        {
            valor = valorEncontrado;
            return true;
        }

        valor = string.Empty;
        return false;
    }

    private static string ObtenerTextoRequerido(
        JsonElement elemento,
        string propiedad)
    {
        if (!elemento.TryGetProperty(propiedad, out var valor) ||
            valor.ValueKind != JsonValueKind.String)
        {
            throw new ReglaAutorizacionInvalidaException(
                $"La propiedad '{propiedad}' es obligatoria.");
        }

        return valor.GetString() ?? string.Empty;
    }

    private static bool ValorCoincide(
        string valorIzquierdo,
        JsonElement valorDerecho)
    {
        return valorDerecho.ValueKind switch
        {
            JsonValueKind.String => string.Equals(
                valorIzquierdo,
                valorDerecho.GetString(),
                StringComparison.OrdinalIgnoreCase),
            JsonValueKind.Number => TryConvertirDecimal(valorIzquierdo, out var numeroIzquierdo) &&
                TryConvertirDecimal(valorDerecho, out var numeroDerecho) &&
                numeroIzquierdo == numeroDerecho,
            JsonValueKind.True => bool.TryParse(valorIzquierdo, out var booleano) && booleano,
            JsonValueKind.False => bool.TryParse(valorIzquierdo, out var booleano) && !booleano,
            _ => false
        };
    }

    private static bool TryConvertirDecimal(
        JsonElement valor,
        out decimal numero)
    {
        if (valor.ValueKind == JsonValueKind.Number)
        {
            return valor.TryGetDecimal(out numero);
        }

        if (valor.ValueKind == JsonValueKind.String)
        {
            return TryConvertirDecimal(valor.GetString() ?? string.Empty, out numero);
        }

        numero = default;
        return false;
    }

    private static bool TryConvertirDecimal(
        string valor,
        out decimal numero)
    {
        return decimal.TryParse(
            valor,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out numero);
    }

    private static bool TryConvertirHora(
        string valor,
        out TimeOnly hora)
    {
        return TimeOnly.TryParseExact(
            valor,
            "HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out hora);
    }
}
