# Transaction Service

Event-driven microservice for processing financial transactions with Kafka integration and anti-fraud validation.

## Architecture Overview

This service implements a Clean Architecture pattern with Domain-Driven Design (DDD) principles, featuring:

- **Lambda/HTTP API**: REST endpoint for transaction creation
- **Kafka Integration**: Event-driven communication with AntiFraud Service
- **Worker Service**: Background consumer for transaction status updates
- **PostgreSQL**: Persistent storage for transaction data

## Technology Stack

- **.NET 8**: Core framework
- **PostgreSQL**: Database
- **Apache Kafka**: Message broker (with Zookeeper)
- **Confluent.Kafka**: Kafka client library
- **Entity Framework Core**: ORM
- **Newtonsoft.Json**: JSON serialization
- **MediatR**: CQRS pattern implementation

## Project Structure

```
app/
├── src/
│   ├── Domain/              # Business entities and enums
│   ├── Application/         # Commands, queries, DTOs, interfaces
│   ├── Infrastructure/      # Database, repositories, Kafka publisher
│   ├── Lambda/              # HTTP API entry point (port 5050)
│   └── Worker/              # Background service for Kafka consumers
├── tests/
│   └── Application.Tests/   # xUnit tests for Application layer
├── docker-compose.yml       # Infrastructure services
├── openapi.yml             # API specification
└── sequence-diagram.md      # Architecture diagrams
```

## Getting Started

### Prerequisites

- .NET 8 SDK
- Docker Desktop
- PostgreSQL client (optional, for manual DB access)

### Infrastructure Setup

1. Start required services (PostgreSQL, Kafka, Zookeeper):

```powershell
cd app
docker-compose up -d
```

2. Verify services are running:

```powershell
docker ps
```

Expected containers:
- PostgreSQL (port 5432)
- Kafka (port 9092)
- Zookeeper (port 2181)

### Running the Application

#### 1. Start the Transaction Service (HTTP API)

```powershell
cd src/Lambda
dotnet run
```

The API will be available at: `http://localhost:5050`

#### 2. Start the Worker Service (Kafka Consumer)

```powershell
cd src/Worker
dotnet run
```

## API Documentation

### Create Transaction

**Endpoint**: `POST /`  
**Port**: `5050`

**Request Body**:
```json
{
  "sourceAccountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "targetAccountId": "4fb96f75-6828-5673-c4gd-3d074g77bgb7",
  "transferTypeId": 1,
  "value": 100.50
}
```

**Success Response** (200):
```json
{
  "transactionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "status": "pending"
}
```

**Error Response** (400):
```json
{
  "error": "Value must be greater than 0"
}
```

See `openapi.yml` for complete API specification.

## Event Flow

### Transaction Creation Flow

1. **Client** → POST request to Transaction Service
2. **Transaction Service** → Saves transaction (status: pending) → Publishes `TransactionCreatedEvent` to Kafka
3. **AntiFraud Service** → Consumes event → Analyzes transaction → Publishes `TransactionStatusEvent`
4. **Worker Service** → Consumes status event → Updates transaction in database
5. **Database** → Transaction status updated (approved/rejected)

### Kafka Topics

- `transaction-events`: Transaction creation events
- `transaction-status-events`: Status update events from AntiFraud

### Event Schemas

**TransactionCreatedEvent**:
```json
{
  "transactionId": "guid",
  "sourceAccountId": "guid",
  "targetAccountId": "guid",
  "transferTypeId": 1,
  "value": 100.50,
  "createdDate": "2025-10-24T10:30:00Z"
}
```

**TransactionStatusEvent**:
```json
{
  "transactionId": "guid",
  "status": 2,
  "rejectionReason": "optional string"
}
```

## Configuration

### Database Connection

Edit `appsettings.json` in Lambda and Worker projects:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=transactionsdb;Username=postgres;Password=misql"
  }
}
```

### Kafka Configuration

```json
{
  "ConnectionStrings": {
    "Kafka": "localhost:9092"
  }
}
```

## Testing with Postman

1. Import the OpenAPI specification (`openapi.yml`)
2. Create a POST request to `http://localhost:5050/`
3. Set request body (JSON) with required fields
4. Send request and verify response

## Transaction Status Enum

| Value | Status    | Description |
|-------|-----------|-------------|
| 1     | Pending   | Awaiting fraud analysis |
| 2     | Approved  | Passed fraud checks |
| 3     | Rejected  | Failed fraud checks |

## Development Notes

### Case-Insensitive JSON Deserialization

The Worker service uses `PropertyNameCaseInsensitive = true` to handle both PascalCase and camelCase JSON from different services.

### Kafka Consumer Groups

Worker uses consumer group `transaction-service-group` with `AutoOffsetReset = Latest` to process only new messages.

## Troubleshooting

### Port Already in Use

If port 5050 is occupied, modify `Program.cs` in Lambda project:

```csharp
var url = "http://localhost:5050/";  // Change port here
```

### Kafka Connection Issues

Verify Kafka is running:
```powershell
docker logs <kafka-container-id>
```

### Database Migration

The application uses automatic migrations. Ensure PostgreSQL is accessible and credentials are correct.

## Testing

### Running Unit Tests

The project includes comprehensive unit tests for the Application layer using xUnit, Moq, and FluentAssertions.

**Quick Test Run:**
```powershell
cd app/tests/Application.Tests
dotnet test
```

**With Coverage Report (Recommended):**
```powershell
cd app/tests/Application.Tests
.\run-tests-with-coverage.ps1
```

### Test Coverage

- **Total Tests**: 29 passing
- **Line Coverage**: 48.38% (100% on business logic)
- **Branch Coverage**: 100%

**Covered Components:**
- ✅ `TransactionCommands` - 100%
- ✅ `TransactionService` - 100%
- ✅ `Transaction` (Entity) - 100%
- ✅ `TransactionCreatedEvent` - 93.33%

See [Application.Tests/README.md](app/tests/Application.Tests/README.md) for detailed test documentation.

## License

This project is part of an educational examination system.
