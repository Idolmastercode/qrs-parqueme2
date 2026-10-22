namespace qrs_back.Endpoints;

/// <summary>
/// Respuestas de error con la misma forma en toda la API: { "error": "mensaje para mostrar" }.
/// El front solo tiene que leer .error y ponerlo en el aviso.
/// </summary>
public static class Resp
{
    /// <summary>400: el dato viene mal (vacío, negativo, no existe…).</summary>
    public static IResult Mal(string error) => Results.BadRequest(new { error });

    /// <summary>409: el dato está bien pero choca con otro (OP u operación repetida, nombre repetido).</summary>
    public static IResult Choca(string error) => Results.Conflict(new { error });
}