namespace Musoq.Benchmarks.Schema.Profiles;

public class ProfileEntity
{
    public ProfileEntity(string firstName,
        string lastName,
        string email,
        string gender,
        string ipAddress,
        string date,
        string image,
        string animal,
        string avatar)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Gender = gender;
        IpAddress = ipAddress;
        Date = date;
        Image = image;
        Animal = animal;
        Avatar = avatar;
    }

    internal ProfileEntity()
    {
        
    }

    public static readonly IDictionary<string, int> KNameToIndexMap;
    public static readonly IDictionary<int, Func<ProfileEntity, object>> KIndexToObjectAccessMap;

    static ProfileEntity()
    {
        KNameToIndexMap = new Dictionary<string, int>
        {
            {nameof(FirstName), 11},
            {nameof(LastName), 12},
            {nameof(Email), 13},
            {nameof(Gender), 14},
            {nameof(IpAddress), 15},
            {nameof(Date), 16},
            {nameof(Image), 17},
            {nameof(Animal), 18},
            {nameof(Avatar), 19}
        };

        KIndexToObjectAccessMap = new Dictionary<int, Func<ProfileEntity, object>>
        {
            {11, arg => arg.FirstName},
            {12, arg => arg.LastName},
            {13, arg => arg.Email},
            {14, arg => arg.Gender},
            {15, arg => arg.IpAddress},
            {16, arg => arg.Date},
            {17, arg => arg.Image},
            {18, arg => arg.Animal},
            {19, arg => arg.Avatar}
        };
    }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public required string Email { get; init; }

    public required string Gender { get; init; }

    public required string IpAddress { get; init; }

    public required string Date { get; init; }

    public required string Image { get; init; }

    public required string Animal { get; init; }

    public required string Avatar { get; init; }
}