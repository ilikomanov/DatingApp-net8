namespace API.Extensions;

public static class DateTimeExtensions
{
    public static int CalculateAge(this DateOnly dob) //dob - DateOfBirth
    {
        var today = DateOnly.FromDateTime(DateTime.Now);

        var age = today.Year - dob.Year;

        if (dob > today.AddYears(-age)) age--;
        //this method won't be accurate on leap years - if the birthday is on 29 of February.
        return age;
    }
}
