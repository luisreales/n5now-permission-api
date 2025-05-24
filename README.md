# N5Now Permission API

A .NET 8.0-based API for managing employee permissions, built with clean architecture principles and modern technologies.

## Features

- Request, modify, and retrieve employee permissions
- Event-driven architecture using Kafka
- Elasticsearch integration for search capabilities
- SQLite database for data persistence
- Swagger/OpenAPI documentation
- Comprehensive logging with Serilog

## Prerequisites

- .NET 8.0 SDK
- Docker and Docker Compose
- SQLite (for local development)
- Kafka (provided via Docker)
- Elasticsearch (provided via Docker)

## Project Structure

```
N5Now/
├── N5Now.Api/              # Web API project
├── N5Now.Application/      # Application layer (use cases, DTOs)
├── N5Now.Domain/          # Domain layer (entities, interfaces)
├── N5Now.Infrastructure/  # Infrastructure layer (implementations)
└── N5Now.Tests/           # Unit tests
```

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd n5now-permission-api
```

### 2. Run with Docker Compose

The easiest way to run the application is using Docker Compose, which will set up all required services:

```bash
docker-compose up -d
```

This will start:
- The N5Now API
- Kafka
- Elasticsearch
- SQLite database

### 3. Run Locally

If you prefer to run the application locally:

1. Install dependencies:
```bash
dotnet restore
```

2. Update the connection strings in `appsettings.json`:
```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  }
}
```

3. Run the application:
```bash
cd N5Now.Api
dotnet run
```

### 4. Access the API

- Swagger UI: `http://localhost:5000/swagger`
- API Base URL: `http://localhost:5000/api`

## API Endpoints

- `POST /api/permissions` - Request a new permission
- `PUT /api/permissions/{id}` - Modify an existing permission
- `GET /api/permissions` - Get all permissions

## Development

### Running Tests

```bash
dotnet test
```

### Database Migrations

```bash
cd N5Now.Api
dotnet ef migrations add <migration-name>
dotnet ef database update
```

## Testing

The project includes both unit tests and integration tests. Here's how to run them:

### Running Tests

#### Using .NET CLI

1. **Run all tests**:
```bash
dotnet test
```

2. **Run only unit tests**:
```bash
dotnet test --filter "Category=Unit"
```

3. **Run only integration tests**:
```bash
dotnet test --filter "Category=Integration"
```

4. **Run a specific test class**:
```bash
dotnet test --filter "FullyQualifiedName=N5Now.Tests.Integration.PermissionIntegrationTests"
```

5. **Run a specific test method**:
```bash
dotnet test --filter "FullyQualifiedName=N5Now.Tests.Integration.PermissionIntegrationTests.RequestPermission_ValidData_ShouldPersistAndPublishEvent"
```

6. **Run tests with detailed output**:
```bash
dotnet test -v n
```

7. **Run tests with coverage report**:
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov /p:CoverletOutput=./lcov.info
```

#### Using Visual Studio

1. Open Test Explorer (View -> Test Explorer)
2. Tests are organized by category (Unit/Integration)
3. You can:
   - Run all tests
   - Run tests in a specific category
   - Run a specific test class
   - Run a specific test method

#### Using VS Code

1. Install the ".NET Core Test Explorer" extension
2. Open the Testing panel (View -> Testing)
3. Tests are organized by category
4. Click the play button next to any test to run it

### Test Categories

- **Unit Tests**: Located in `N5Now.Tests/Unit/`
  - Test individual components in isolation
  - Use mocks for external dependencies
  - Fast execution

- **Integration Tests**: Located in `N5Now.Tests/Integration/`
  - Test complete workflows
  - Use in-memory database
  - Mock external services (Kafka, Elasticsearch)
  - Verify system integration

### Test Coverage

To generate a test coverage report:

1. Install the coverlet collector:
```bash
dotnet tool install --global coverlet.console
```

2. Run tests with coverage:
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov /p:CoverletOutput=./lcov.info
```

3. View the coverage report in your IDE or using a tool like ReportGenerator

## Technologies Used

- .NET 8.0
- Entity Framework Core
- MediatR (CQRS pattern)
- Kafka
- Elasticsearch
- SQLite
- Serilog
- xUnit (testing)
- Moq (mocking)

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details. 