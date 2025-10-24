using System.Net;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Lambda
{
    class Program
    {
        private static IConfiguration? _configuration;

        static async Task Main(string[] args)
        {
            // Cargar configuración
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var port = 5050;
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();

            Console.WriteLine($"Transaction Service running on http://localhost:{port}/");

            var function = new Function();

            while (true)
            {
                try
                {
                    var context = await listener.GetContextAsync();
                    _ = Task.Run(async () => await HandleRequest(context, function));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Server Error: {ex.Message}");
                }
            }
        }

        static async Task HandleRequest(HttpListenerContext context, Function function)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                // Validar JWT Token
                var principal = ValidateJwtToken(request);
                if (principal == null)
                {
                    Console.WriteLine($"Unauthorized access attempt from {request.RemoteEndPoint}");
                    await SendResponse(response, 401, new 
                    { 
                        error = "Unauthorized",
                        message = "Invalid or missing JWT token. Include 'Authorization: Bearer <token>' header."
                    });
                    return;
                }

                // Obtener información del usuario del token
                var userName = principal.FindFirst("name")?.Value ?? "Unknown";
                var userRole = principal.FindFirst("role")?.Value ?? "Unknown";
                Console.WriteLine($"Authenticated request from: {userName} (Role: {userRole})");

                if (request.HttpMethod != "POST")
                {
                    await SendResponse(response, 405, new { error = "Only POST method allowed" });
                    return;
                }

                string body;
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    body = await reader.ReadToEndAsync();
                }

                var lambdaRequest = JsonConvert.DeserializeObject<Function.CreateTransactionRequest>(body);
                
                if (lambdaRequest == null)
                {
                    await SendResponse(response, 400, new { error = "Invalid JSON" });
                    return;
                }

                var result = await function.Handler(lambdaRequest, null!);
                await SendResponse(response, 200, result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                await SendResponse(response, 500, new { error = ex.Message });
            }
        }

        static ClaimsPrincipal? ValidateJwtToken(HttpListenerRequest request)
        {
            try
            {
                // Obtener el header Authorization
                var authHeader = request.Headers["Authorization"];
                
                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                // Extraer el token (después de "Bearer ")
                var token = authHeader.Substring(7);

                // Obtener configuración JWT
                var secretKey = _configuration?["Jwt:SecretKey"];
                var issuer = _configuration?["Jwt:Issuer"];
                var audience = _configuration?["Jwt:Audience"];

                if (string.IsNullOrWhiteSpace(secretKey))
                {
                    Console.WriteLine("Error: JWT SecretKey not configured");
                    return null;
                }

                // Configurar parámetros de validación
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(secretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true, // Validar expiración
                    ClockSkew = TimeSpan.Zero // Sin tolerancia de tiempo
                };

                // Validar el token
                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                
                return principal;
            }
            catch (SecurityTokenExpiredException)
            {
                Console.WriteLine("Token expired");
                return null;
            }
            catch (SecurityTokenException ex)
            {
                Console.WriteLine($"Invalid token: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token validation error: {ex.Message}");
                return null;
            }
        }

        static async Task SendResponse(HttpListenerResponse response, int statusCode, object data)
        {
            try
            {
                response.StatusCode = statusCode;
                response.ContentType = "application/json";
                var json = JsonConvert.SerializeObject(data);
                var bytes = Encoding.UTF8.GetBytes(json);
                await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            }
            finally
            {
                response.Close();
            }
        }
    }
}
