namespace qrs_back;

/// <summary>
/// Hora de México (centro). Las citas se guardan en hora local, así que para saber si una
/// ya pasó hay que compararla con la hora de México, no con la del servidor (que puede estar en UTC).
/// Desde 2022 México ya no cambia de horario: siempre es UTC−6.
/// </summary>
public static class Hora
{
    public static DateTime Mexico() => DateTime.SpecifyKind(DateTime.UtcNow.AddHours(-6), DateTimeKind.Unspecified);
}
