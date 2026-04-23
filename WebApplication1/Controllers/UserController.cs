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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest("Invalid user ID.");
            }

            try {
                var user = await _userService.GetUser(id);
                if (user == null)
                {
                    return NotFound();
                }

                return Ok(user);
            } catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving the user.");
            }
           
        }


        [HttpPost]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (user == null)
            {
                return BadRequest("User payload cannot be null.");
            }
                
            try
            {
                var createdUser = await _userService.RegisterUser(user);
                if (createdUser == null)
                {
                    return BadRequest("User could not be created.");
                }
                return Ok(createdUser);
            }
            catch (Exception e)
            {
                Console.WriteLine($"HandlerException: {e.Message}");
                return StatusCode(500, "An error occurred while creating the user.");
            }
        }





      



    }
}
