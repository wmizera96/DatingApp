namespace API.Extensions;

public static class DateTimeExtensions
{
    public static int CalculateAge(this DateOnly dateOnly)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dateOnly.Year;

        if (dateOnly > today.AddYears(-age)) age--;

        return age;
    }
}