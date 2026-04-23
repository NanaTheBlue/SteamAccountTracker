using WebApplication1.Models;
using WebApplication1.Repository;

namespace WebApplication1.Services
{
    public class UserService: IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<User?> GetUser(Guid id)
        {
            
                return await _userRepository.GetById(id);

           
         
        }
        public async Task<User?> RegisterUser(User user)
        {
            return await _userRepository.RegisterUser(user);
        }
    }


        
    }

