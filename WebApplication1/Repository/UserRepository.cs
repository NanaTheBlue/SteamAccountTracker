using Microsoft.Data.SqlClient;
using System.Data.Common;
using System.Transactions;
using WebApplication1.Models;


namespace WebApplication1.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;

        public UserRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not found.");
        }





        public  async Task<User?> GetUserFromEmail(string email)
        {


            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            try
            {
                using var cmd = new SqlCommand("SELECT Id, Username, Email, Passwordhash FROM Users WHERE Email = @Email;", conn);
                cmd.Parameters.AddWithValue("@Email", email);
                using var reader = await cmd.ExecuteReaderAsync();
                var idOrdinal = reader.GetOrdinal("Id");
                var usernameOrdinal = reader.GetOrdinal("Username");
                var emailOrdinal = reader.GetOrdinal("Email");
                var passwordHashOrdinal = reader.GetOrdinal("Passwordhash");
                if (await reader.ReadAsync())
                {
                    return new User
                    {
                        ID = reader.GetGuid(idOrdinal),
                        Username = reader.GetString(usernameOrdinal),
                        Email = reader.GetString(emailOrdinal),
                        PasswordHash = reader.GetString(passwordHashOrdinal)
                    };
                }
                return null;
            }
            catch (SqlException e)
            {
                Console.WriteLine($"SqlException: {e.Message}");
                throw;
            }
        }

        public async Task<UserDto?> RegisterUser(User user)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();


            try
            {

                using var cmd = new SqlCommand("INSERT INTO Users (Username, Email, Passwordhash)  OUTPUT  inserted.Id, inserted.Username, inserted.Email VALUES (@username, @email, @passwordhash);", conn);
                cmd.Parameters.AddWithValue("@username", user.Username);
                cmd.Parameters.AddWithValue("@email", user.Email);
                cmd.Parameters.AddWithValue("@passwordhash", user.PasswordHash);

                using var reader = await cmd.ExecuteReaderAsync();
                var idOrdinal = reader.GetOrdinal("Id");
                var usernameOrdinal = reader.GetOrdinal("Username");
                var emailOrdinal = reader.GetOrdinal("Email");
                if (await reader.ReadAsync())
                {
                    return new UserDto
                    {
                        ID = reader.GetGuid(idOrdinal),
                        Username = reader.GetString(usernameOrdinal),
                        Email = reader.GetString(emailOrdinal),
                    };

                }


                return null;
            }

            catch (SqlException e) when (e.Number == 2627)
            {

                return null;
            }
            catch (SqlException e)
            {
                Console.WriteLine($"SqlException: {e.Message}");
                throw;
            }




        }





        public async Task<AuthenticatedUser?> GetUserFromSession(Guid id)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            

            try
            {
               
                using var cmd = new SqlCommand("SELECT Id, Session_exp FROM Users WHERE Session_Id = @session_id;", conn);
                cmd.Parameters.AddWithValue("@session_id", id);
                using var reader = await cmd.ExecuteReaderAsync();

                var idOrdinal = reader.GetOrdinal("Id");
               
                var sessionExpOrdinal = reader.GetOrdinal("Session_exp");
              

                if (await reader.ReadAsync())
                {
                    return new AuthenticatedUser
                    {

                        Id = reader.GetGuid(idOrdinal),
                       
                        SessionExp = reader.GetDateTime(sessionExpOrdinal)
                    };



                }

                // in sql server transactions are rolled back automatically on error for single statements

                return null;
            }
            catch (SqlException e)
            {
                Console.WriteLine($"SqlException: {e.Message}");
                throw;
            }
           
            
            
           
        }
    }
}
