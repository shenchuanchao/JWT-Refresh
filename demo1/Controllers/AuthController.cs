using demo1.Models;
using demo1.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ITokenService _tokenService;

        public AuthController(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // 简单模拟用户验证（实际应查询数据库并验证密码哈希）
            if (request.Username != "test" || request.Password != "123456")
            {
                return Unauthorized("用户名或密码错误");
            }

            var tokens = _tokenService.GenerateTokens(request.Username);
            return Ok(tokens);
        }

        // POST: api/auth/refresh
        [HttpPost("refresh")]
        public IActionResult Refresh([FromBody] RefreshRequest request)
        {
            var newTokens = _tokenService.RefreshTokens(request.RefreshToken);
            if (newTokens == null)
            {
                return Unauthorized("无效的刷新令牌");
            }
            return Ok(newTokens);
        }

        // POST: api/auth/logout
        [Authorize] // 需要有效的 Access Token
        [HttpPost("logout")]
        public IActionResult Logout([FromBody] RefreshRequest request)
        {
            // 简单移除该刷新令牌（也可不传，直接由客户端丢弃，但为了立即失效，可调用撤销）
            // 这里通过 RefreshTokens 服务，将旧令牌失效（因为我们未保存多个会话，所以直接调用刷新会使其失效）
            // 更好的做法是提供一个专门撤销单个刷新令牌的方法，此处为了简化，直接复用 RefreshTokens 并丢弃结果
            _tokenService.RefreshTokens(request.RefreshToken);
            return Ok(new { message = "已退出" });
        }

        // POST: api/auth/revoke-all
        [Authorize(Roles = "Admin")] // 假设管理员角色才能执行强制退出
        [HttpPost("revoke-all")]
        public IActionResult RevokeAll([FromBody] RevokeRequest request)
        {
            _tokenService.RevokeUserTokens(request.Username);
            return Ok(new { message = $"用户 {request.Username} 的所有会话已被强制退出" });
        }

        // GET: api/auth/me (受保护资源示例)
        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var username = User.Identity?.Name;
            return Ok(new { Username = username });
        }
    }
}
