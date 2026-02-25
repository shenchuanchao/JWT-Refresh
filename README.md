> .NET 8 的 Web API 演示项目，完整实现方案三的核心逻辑：短时 Access Token + 服务端存储的 Refresh Token，并支持“强制退出”（撤销特定用户的所有会话）。该 Demo 使用内存缓存模拟服务器端存储，便于你快速理解与测试。
## 1. 项目结构与依赖
. 框架：.NET 8 Web API

. 核心 NuGet 包：

 . Microsoft.AspNetCore.Authentication.JwtBearer

. 存储模拟：IMemoryCache（内存缓存）

项目主要包含以下部分：

```text
/Controllers
    - AuthController.cs          // 登录、刷新、退出、撤销会话
/Services
    - ITokenService.cs            // 令牌服务接口
    - TokenService.cs              // 实现生成 Access Token、管理 Refresh Token
/Models
    - LoginRequest.cs              // 登录请求模型
    - RefreshRequest.cs            // 刷新请求模型
    - RevokeRequest.cs             // 撤销请求模型
    - TokenResponse.cs             // 令牌响应模型
Program.cs                         // 配置服务与中间件
appsettings.json                   // JWT 配置
```