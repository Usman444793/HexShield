#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.AspNetCore.Identity, 2.2.0"
#r "nuget: Microsoft.Data.SqlClient, 5.2.2"

using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

var connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=HexShieldDb;Trusted_Connection=True;TrustServerCertificate=True;";
var targetEmail = "Admin@hexshield.com";
var newPassword = "Admin@12345678";

// Generate hash using ASP.NET Core Identity v3 hasher
var hasher = new PasswordHasher<object>();
var hash = hasher.HashPassword(new object(), newPassword);

Console.WriteLine($"Generated hash for '{newPassword}':");
Console.WriteLine(hash);

using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();

// Update the password hash and reset access failed count
var cmd = new SqlCommand(@"
    UPDATE AspNetUsers 
    SET PasswordHash = @hash, 
        AccessFailedCount = 0,
        LockoutEnd = NULL
    WHERE NormalizedEmail = @email", connection);

cmd.Parameters.AddWithValue("@hash", hash);
cmd.Parameters.AddWithValue("@email", targetEmail.ToUpperInvariant());

var rows = await cmd.ExecuteNonQueryAsync();
Console.WriteLine($"\nUpdated {rows} row(s) for email '{targetEmail}'.");
Console.WriteLine($"Password has been reset to: {newPassword}");
Console.WriteLine("AccessFailedCount reset to 0, LockoutEnd cleared.");
