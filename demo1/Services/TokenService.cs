using demo1.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace demo1.Services
{
    public class TokenService : ITokenService
    {
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;

        // 内存缓存中存储的键：RefreshToken -> Username
        private const string RefreshTokenCachePrefix = "refresh_";

        public TokenService(IMemoryCache cache, IConfiguration configuration)
        {
            _cache = cache;
            _configuration = configuration;
        }

        public TokenResponse GenerateTokens(string username)
        {
            // 1. 生成 Access Token (JWT)
            var accessToken = GenerateAccessToken(username);

            // 2. 生成 Refresh Token (随机字符串)
            var refreshToken = GenerateRefreshToken();

            // 3. 将 Refresh Token 存入缓存，关联到用户名
            var cacheKey = $"{RefreshTokenCachePrefix}{refreshToken}";
            _cache.Set(cacheKey, username, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7) // Refresh Token 有效期 7 天
            });

            // 为了方便撤销，可以同时维护一个“用户名 -> 刷新令牌列表”的映射
            // 这里采用另一种方式：撤销时遍历所有缓存键，性能不高但简单。
            // 更好的做法是维护一个 Dictionary<string, List<string>>，此处为了简洁不做额外优化。
            // 你也可以使用双重缓存：一个存 username->tokens，一个存 token->username。
            // 我们将在 RevokeUserTokens 中通过遍历缓存实现撤销，生产环境建议使用 Redis 等支持按模式查找的存储。

            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes())
            };
        }

        public TokenResponse? RefreshTokens(string refreshToken)
        {
            var cacheKey = $"{RefreshTokenCachePrefix}{refreshToken}";
            if (_cache.TryGetValue(cacheKey, out string? username) && username != null)
            {
                // 刷新令牌有效，生成新令牌对（同时使旧刷新令牌失效）
                // 注意：根据需求，可以删除旧的刷新令牌并生成新的，以实现“单次使用”刷新令牌
                // 这里演示最简单的轮换：使用旧刷新令牌换取新令牌对，同时删除旧令牌
                _cache.Remove(cacheKey);
                return GenerateTokens(username);
            }

            return null; // 无效刷新令牌
        }

        public void RevokeUserTokens(string username)
        {
            // 由于内存缓存不支持按值搜索，这里我们只能遍历所有缓存项（仅演示用）
            // 实际生产请使用 Redis 的 Set 或数据库存储用户与刷新令牌的映射
            var cacheEntriesField = typeof(MemoryCache).GetField("_entries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (cacheEntriesField?.GetValue(_cache) is IDictionary<object, object> entries)
            {
                var keysToRemove = new List<string>();
                foreach (var entry in entries)
                {
                    var key = entry.Key.ToString();
                    if (key != null && key.StartsWith(RefreshTokenCachePrefix))
                    {
                        var value = entry.Value?.ToString();
                        if (value == username)
                        {
                            keysToRemove.Add(key);
                        }
                    }
                }
                foreach (var key in keysToRemove)
                {
                    _cache.Remove(key);
                }
            }
        }

        private string GenerateAccessToken(string username)
        {
            var claims = new[]
            {
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);
            var credentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes()),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private double GetAccessTokenExpirationMinutes()
        {
            return _configuration.GetValue<double>("Jwt:AccessTokenExpirationMinutes");
        }
    }
}
