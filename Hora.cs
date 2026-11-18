namespace qrs_back;

public static class Hora
{
    public static DateTime Mexico() => DateTime.SpecifyKind(DateTime.UtcNow.AddHours(-6), DateTimeKind.Unspecified);
}
