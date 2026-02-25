using demo1.Models;

namespace demo1.Services
{
    public interface ITokenService
    {
        /// <summary>
        /// 为指定用户生成新的访问令牌和刷新令牌
        /// </summary>
        TokenResponse GenerateTokens(string username);

        /// <summary>
        /// 使用刷新令牌换取新的访问令牌
        /// </summary>
        /// <returns>新令牌对，若刷新令牌无效则返回 null</returns>
        TokenResponse? RefreshTokens(string refreshToken);

        /// <summary>
        /// 撤销指定用户的所有刷新令牌（强制退出）
        /// </summary>
        void RevokeUserTokens(string username);
    }

}
