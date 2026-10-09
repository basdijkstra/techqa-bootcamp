namespace PixelVault.Shipping.Domain;

/// <summary>Reference data for the countries PixelVault ships to.</summary>
public static class Countries
{
    public const string UnitedStates = "US";
    public const string Canada = "CA";

    public static readonly IReadOnlyList<Region> UsStates =
    [
        new("AL", "Alabama"), new("AK", "Alaska"), new("AZ", "Arizona"), new("AR", "Arkansas"),
        new("CA", "California"), new("CO", "Colorado"), new("CT", "Connecticut"), new("DE", "Delaware"),
        new("DC", "District of Columbia"), new("FL", "Florida"), new("GA", "Georgia"), new("HI", "Hawaii"),
        new("ID", "Idaho"), new("IL", "Illinois"), new("IN", "Indiana"), new("IA", "Iowa"),
        new("KS", "Kansas"), new("KY", "Kentucky"), new("LA", "Louisiana"), new("ME", "Maine"),
        new("MD", "Maryland"), new("MA", "Massachusetts"), new("MI", "Michigan"), new("MN", "Minnesota"),
        new("MS", "Mississippi"), new("MO", "Missouri"), new("MT", "Montana"), new("NE", "Nebraska"),
        new("NV", "Nevada"), new("NH", "New Hampshire"), new("NJ", "New Jersey"), new("NM", "New Mexico"),
        new("NY", "New York"), new("NC", "North Carolina"), new("ND", "North Dakota"), new("OH", "Ohio"),
        new("OK", "Oklahoma"), new("OR", "Oregon"), new("PA", "Pennsylvania"), new("RI", "Rhode Island"),
        new("SC", "South Carolina"), new("SD", "South Dakota"), new("TN", "Tennessee"), new("TX", "Texas"),
        new("UT", "Utah"), new("VT", "Vermont"), new("VA", "Virginia"), new("WA", "Washington"),
        new("WV", "West Virginia"), new("WI", "Wisconsin"), new("WY", "Wyoming")
    ];

    public static readonly IReadOnlyList<Region> CanadianProvinces =
    [
        new("AB", "Alberta"), new("BC", "British Columbia"), new("MB", "Manitoba"), new("NB", "New Brunswick"),
        new("NL", "Newfoundland and Labrador"), new("NS", "Nova Scotia"), new("NT", "Northwest Territories"),
        new("NU", "Nunavut"), new("ON", "Ontario"), new("PE", "Prince Edward Island"), new("QC", "Quebec"),
        new("SK", "Saskatchewan"), new("YT", "Yukon")
    ];

    public static readonly IReadOnlyList<CountryInfo> All =
    [
        new(UnitedStates, "United States", "State", "ZIP code", "10001 or 10001-1234", UsStates),
        new(Canada, "Canada", "Province", "Postal code", "K1A 0B1", CanadianProvinces)
    ];

    /// <summary>
    /// First three digits of the ZIP codes in each state (simplified). Used to check that a ZIP code
    /// belongs to the selected state.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (int From, int To)[]> UsZipPrefixes = new Dictionary<string, (int, int)[]>
    {
        ["AL"] = [(350, 369)], ["AK"] = [(995, 999)], ["AZ"] = [(850, 865)], ["AR"] = [(716, 729)],
        ["CA"] = [(900, 961)], ["CO"] = [(800, 816)], ["CT"] = [(60, 69)], ["DE"] = [(197, 199)],
        ["DC"] = [(200, 205)], ["FL"] = [(320, 349)], ["GA"] = [(300, 319), (398, 399)], ["HI"] = [(967, 968)],
        ["ID"] = [(832, 838)], ["IL"] = [(600, 629)], ["IN"] = [(460, 479)], ["IA"] = [(500, 528)],
        ["KS"] = [(660, 679)], ["KY"] = [(400, 427)], ["LA"] = [(700, 714)], ["ME"] = [(39, 49)],
        ["MD"] = [(206, 219)], ["MA"] = [(10, 27)], ["MI"] = [(480, 499)], ["MN"] = [(550, 567)],
        ["MS"] = [(386, 397)], ["MO"] = [(630, 658)], ["MT"] = [(590, 599)], ["NE"] = [(680, 693)],
        ["NV"] = [(889, 898)], ["NH"] = [(30, 38)], ["NJ"] = [(70, 89)], ["NM"] = [(870, 884)],
        ["NY"] = [(100, 149), (5, 5)], ["NC"] = [(270, 289)], ["ND"] = [(580, 588)], ["OH"] = [(430, 459)],
        ["OK"] = [(730, 749)], ["OR"] = [(970, 979)], ["PA"] = [(150, 196)], ["RI"] = [(28, 29)],
        ["SC"] = [(290, 299)], ["SD"] = [(570, 577)], ["TN"] = [(370, 385)], ["TX"] = [(750, 799), (885, 885)],
        ["UT"] = [(840, 847)], ["VT"] = [(50, 59)], ["VA"] = [(220, 246), (201, 201)], ["WA"] = [(980, 994)],
        ["WV"] = [(247, 268)], ["WI"] = [(530, 549)], ["WY"] = [(820, 831)]
    };

    /// <summary>The first letter of a Canadian postal code tells you the province.</summary>
    public static readonly IReadOnlyDictionary<string, string> CanadianPostalPrefixes = new Dictionary<string, string>
    {
        ["NL"] = "A", ["NS"] = "B", ["PE"] = "C", ["NB"] = "E", ["QC"] = "GHJ", ["ON"] = "KLMNP",
        ["MB"] = "R", ["SK"] = "S", ["AB"] = "T", ["BC"] = "V", ["NT"] = "X", ["NU"] = "X", ["YT"] = "Y"
    };
}
