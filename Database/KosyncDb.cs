namespace Kosync.Database;

public class KosyncDb
{
    public LiteDatabase Context { get; } = default!;

    public KosyncDb()
    {
        Context = new LiteDatabase("Filename=data/Kosync.db;Connection=shared");
        CreateDefaults();
    }

    public void CreateDefaults()
    {
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
        if (adminPassword is null)
        {
            adminPassword = "admin";
        }

        ILiteCollection<User>? userCollection = Context.GetCollection<User>("users");

        User? adminUser = userCollection.FindOne(i => i.Username == "admin");
        if (adminUser is null)
        {
            adminUser = new User()
            {
                Username = "admin",
                IsAdministrator = true,
            };
            userCollection.Insert(adminUser);
        }

        adminUser.PasswordHash = Utility.HashPassword(adminPassword);

        userCollection.Update(adminUser);
        userCollection.EnsureIndex(i => i.Username);
    }
}