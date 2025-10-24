using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using Application.Commands;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Lambda
{
    public class Function
    {
        private readonly IServiceProvider _serviceProvider;

        public Function()
        {
            var startup = new Startup();
            _serviceProvider = startup.Configure();
        }

        public record CreateTransactionRequest(Guid SourceAccountId, Guid TargetAccountId, int TransferTypeId, decimal Value);

        public async Task<object> Handler(CreateTransactionRequest request, ILambdaContext context)
        {
            try
            {
                if (request is null)
                    return new { success = false, error = "Request is null" };

                if (request.SourceAccountId == Guid.Empty)
                    return new { success = false, error = "SourceAccountId is required" };

                if (request.TargetAccountId == Guid.Empty)
                    return new { success = false, error = "TargetAccountId is required" };

                if (request.Value <= 0)
                    return new { success = false, error = "Value must be greater than zero" };

                using var scope = _serviceProvider.CreateScope();
                var commands = scope.ServiceProvider.GetRequiredService<TransactionCommands>();

                var externalId = await commands.InsertAsync(
                    request.SourceAccountId,
                    request.TargetAccountId,
                    request.TransferTypeId,
                    request.Value
                );

                return new
                {
                    success = true,
                    data = externalId,
                    message = "Transaction created successfully",
                    timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                };
            }
        }
    }
}