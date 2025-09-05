using Microsoft.AspNetCore.Builder;
using SecureInterSolution.Middleware.Middleware;

namespace SecureInterSolution.Middleware.Extensions
{
  public static class ApplicationBuilderExtensions
  {
    public static IApplicationBuilder UseSecureCommunication(this IApplicationBuilder app)
    {
      return app.UseMiddleware<EncryptedCommunicationMiddleware>();
    }
  }
}

