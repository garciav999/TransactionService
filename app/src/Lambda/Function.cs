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
        public record CreateTransactionResponse(Guid TransactionExternalId);

        public async Task<CreateTransactionResponse> Handler(CreateTransactionRequest request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            if (request.Value <= 0) throw new ArgumentException("Value must be greater than zero.", nameof(request.Value));

            using var scope = _serviceProvider.CreateScope();

            var commands = scope.ServiceProvider.GetRequiredService<TransactionCommands>();

            var externalId = await commands.InsertAsync(
                request.SourceAccountId,
                request.TargetAccountId,
                request.TransferTypeId,
                request.Value
            );

            return new CreateTransactionResponse(externalId);
        }
    }
}
