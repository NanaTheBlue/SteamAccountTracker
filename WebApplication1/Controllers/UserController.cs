using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Repository;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {

        private readonly IUserService _userService;
            public UserController(IUserService userService)
        {
            _userService = userService;
        }



        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
        {
            if (registerRequest == null)
            {
                return BadRequest("User payload cannot be null.");
            }
                
            try
            {
                var error = await _userService.RegisterUser(registerRequest);
                if (error != null)
                {
                    return BadRequest("User could not be created.");
                }
                return Ok("User Register");
            }
            catch (Exception e)
            {
                Console.WriteLine($"HandlerException: {e.Message}");
                return StatusCode(500, "An error occurred while creating the user.");
            }
        }


        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            if (loginRequest == null)
            {
                return BadRequest("Request payload cannot be null.");
            }

            try
            {
                var result = await _userService.LoginUser(loginRequest);

                if (result.Success == false) {

                    return BadRequest(result.ErrorMessage);
                }
                

                return Ok("User Login");
            }
            catch (Exception e)
            {
                Console.WriteLine($"HandlerException: {e.Message}");
                return StatusCode(500, "An error occurred while loging in the user.");
            }
        }









    }
}
