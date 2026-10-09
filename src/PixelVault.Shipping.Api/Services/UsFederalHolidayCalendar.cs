using PixelVault.Shipping.Dtos;

namespace PixelVault.Shipping.Services;

public interface IHolidayCalendar
{
    /// <summary>True when carriers don't deliver on this date because it is an (observed) federal holiday.</summary>
    bool IsHoliday(DateOnly date);

    IReadOnlyList<HolidayDto> GetHolidays(int year);
}

/// <summary>
/// The 11 US federal holidays. Fixed-date holidays that fall on a Saturday are observed on the Friday before,
/// and those on a Sunday on the Monday after.
/// </summary>
public class UsFederalHolidayCalendar : IHolidayCalendar
{
    public bool IsHoliday(DateOnly date) =>
        GetHolidays(date.Year).Any(h => h.ObservedOn == date)
        // New Year's Day on a Saturday is observed on Friday December 31 of the year before.
        || GetHolidays(date.Year + 1).Any(h => h.ObservedOn == date);

    public IReadOnlyList<HolidayDto> GetHolidays(int year) =>
    [
        Fixed(year, 1, 1, "New Year's Day"),
        new(NthWeekday(year, 1, DayOfWeek.Monday, 3), NthWeekday(year, 1, DayOfWeek.Monday, 3), "Martin Luther King Jr. Day"),
        new(NthWeekday(year, 2, DayOfWeek.Monday, 3), NthWeekday(year, 2, DayOfWeek.Monday, 3), "Presidents' Day"),
        new(LastWeekday(year, 5, DayOfWeek.Monday), LastWeekday(year, 5, DayOfWeek.Monday), "Memorial Day"),
        Fixed(year, 6, 19, "Juneteenth"),
        Fixed(year, 7, 4, "Independence Day"),
        new(NthWeekday(year, 9, DayOfWeek.Monday, 1), NthWeekday(year, 9, DayOfWeek.Monday, 1), "Labor Day"),
        new(NthWeekday(year, 10, DayOfWeek.Monday, 2), NthWeekday(year, 10, DayOfWeek.Monday, 2), "Columbus Day"),
        Fixed(year, 11, 11, "Veterans Day"),
        new(NthWeekday(year, 11, DayOfWeek.Thursday, 4), NthWeekday(year, 11, DayOfWeek.Thursday, 4), "Thanksgiving Day"),
        Fixed(year, 12, 25, "Christmas Day")
    ];

    private static HolidayDto Fixed(int year, int month, int day, string name)
    {
        var date = new DateOnly(year, month, day);
        var observed = date.DayOfWeek switch
        {
            DayOfWeek.Saturday => date.AddDays(-1),
            DayOfWeek.Sunday => date.AddDays(1),
            _ => date
        };
        return new HolidayDto(date, observed, name);
    }

    /// <summary>The n-th given weekday of a month, e.g. the 3rd Monday of January.</summary>
    private static DateOnly NthWeekday(int year, int month, DayOfWeek dayOfWeek, int n)
    {
        var first = new DateOnly(year, month, 1);
        var offset = ((int)dayOfWeek - (int)first.DayOfWeek + 7) % 7;
        return first.AddDays(offset + (n - 1) * 7);
    }

    private static DateOnly LastWeekday(int year, int month, DayOfWeek dayOfWeek)
    {
        var last = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        var offset = ((int)last.DayOfWeek - (int)dayOfWeek + 7) % 7;
        return last.AddDays(-offset);
    }
}
