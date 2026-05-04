using System.Runtime.InteropServices;
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





        public async Task<AuthenticatedUser?> GetUserFromSession(Guid id)
        {
            var user = await _userRepository.GetUserFromSession(id);

            if (user == null) { return null; }
            // Check if session is expired

            if (user.SessionExp < DateTime.UtcNow)
            {
                return null;
            }


            return user;
        }




        public async Task<LoginResult> LoginUser(LoginRequest LoginRequest)
        {
            try
            {
                var user = await _userRepository.GetUserFromEmail(LoginRequest.Email);
                if (user == null)
                {
                    return new LoginResult { Success = false, ErrorMessage = "Invalid email or password." };
                }

                var result = BC.Verify(LoginRequest.Password, user.PasswordHash);

                if (!result)
                {
                    return new LoginResult { Success = false, ErrorMessage = "Invalid email or password." };
                }

                return new LoginResult
                {
                    Success = true,
                    User = new UserDto
                    {
                        ID = user.ID,
                        Username = user.Username,
                        Email = user.Email
                    }
                };
            }
            catch (Exception ex)
            {
                return new LoginResult { Success = false, ErrorMessage = ex.Message };
            }

        }



        public async Task<UserDto?> RegisterUser(RegisterRequest RegisterRequest)
        {

            if (string.IsNullOrEmpty(RegisterRequest.Username) || string.IsNullOrEmpty(RegisterRequest.Email) || string.IsNullOrEmpty(RegisterRequest.Password))
            {
                throw new ArgumentException("Username, Email and Password are required.");
            }



            if (RegisterRequest.Password.Length < 10)
            {
                throw new Exception("Password Must Be Atleast 10 Characters In Length");
            }

            var salt = BC.GenerateSalt();
            var hashedPW = BC.HashPassword(RegisterRequest.Password, salt );


             var user = new User
            {
                Username = RegisterRequest.Username,
                Email = RegisterRequest.Email,
                PasswordHash = hashedPW,
            };




            var createdUser = await _userRepository.RegisterUser(user);

            if (createdUser == null) {


                return null;
            }


            return new UserDto
            {
                ID = createdUser.ID,
                Username = createdUser.Username,
                Email = createdUser.Email
            };


        }
    }


        
    }

