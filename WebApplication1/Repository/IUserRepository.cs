using Microsoft.Data.SqlClient;
using WebApplication1.Models;

namespace WebApplication1.Repository
{
    public interface IUserRepository
    {
        Task<User?> GetById(Guid id);
        Task<User?> RegisterUser(User user);
    }



}
