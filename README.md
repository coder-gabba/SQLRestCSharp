<div align="center">
  <h1 align="center">SqlAPI - Enterprise REST API with .NET</h1>
  <p align="center">
    A production-ready REST API built with ASP.NET Core 8.0 featuring advanced search, comprehensive testing, automatic database initialization, and enterprise-grade architecture patterns.
  </p>
</div>

<p align="center">
  <img alt="Project Status" src="https://img.shields.io/badge/status-production--ready-brightgreen.svg?style=for-the-badge" />
  <img alt=".NET" src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" />
  <img alt="C#" src="https://img.shields.io/badge/C%23-12-239120?style=for-the-badge&logo=c-sharp&logoColor=white" />
  <img alt="PostgreSQL" src="https://img.shields.io/badge/PostgreSQL-16-4169E1?style=for-the-badge&logo=postgresql&logoColor=white" />
  <img alt="Tests" src="https://img.shields.io/badge/Tests-25%2F25%20Passing-brightgreen?style=for-the-badge" />
  <img alt="License" src="https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge" />
</p>

---

## 🏛️ Architecture Overview

This project follows a clean, layered architecture to ensure separation of concerns and maintainability.

```
+-------------------+      +-------------------+      +-------------------+
|   Controllers     |----->|      Services     |----->|       Data        |
| (API Endpoints)   |      |  (Business Logic) |      | (Database Access) |
+-------------------+      +-------------------+      +-------------------+
        |                          ^                          |
        |                          |                          |
        v                          |                          v
+-------------------+      +-------------------+      +-------------------+
|      Models       |<-----|      (DTOs)       |      |   (EF Core)       |
| (Data Structures) |      +-------------------+      +-------------------+
```

-   **Controllers:** Handle incoming HTTP requests and send responses.
-   **Services:** Contain the core business logic and domain operations (PersonService, JwtService).
-   **Data:** Manages database interactions via Entity Framework Core and custom PostgreSQL handlers.
-   **Models:** Define the data structures (`User`, `Person`, `LoginRequest`).
-   **DTOs:** Data transfer objects for API communication (`PersonSearchDto`, `PagedResultDto`).

---

## 🛠️ Technology Stack

### Core Framework
- **ASP.NET Core 8.0** - High-performance web framework
- **C# 12** - Latest language features and improvements
- **Entity Framework Core 8.0** - Object-relational mapping (ORM)

### Database & Storage
- **PostgreSQL 16+** - Primary database with advanced features
- **In-Memory Database** - Testing and development scenarios

### Security & Authentication
- **JWT Bearer Tokens** - Stateless authentication
- **BCrypt.Net** - Password hashing and salting
- **FluentValidation** - Input validation and sanitization

### Architecture & Patterns
- **Service Layer Pattern** - Clean separation of concerns
- **Repository Pattern** - Data access abstraction
- **Dependency Injection** - Built-in IoC container
- **AutoMapper** - Object-to-object mapping

### Quality & Testing
- **xUnit** - Unit testing framework
- **Moq** - Mocking framework for dependencies
- **FluentAssertions** - Readable test assertions
- **Microsoft.AspNetCore.Mvc.Testing** - Integration testing

### Logging & Monitoring
- **Serilog** - Structured logging with file sinks
- **Performance Counters** - Response time monitoring
- **Exception Handling** - Global error management

### DevOps & Tools
- **Swagger/OpenAPI** - API documentation and testing
- **Polly** - Resilience and transient-fault handling
- **Docker Ready** - Containerization support

---

## ✨ Features

### 🚀 Core Functionality
-   **Full CRUD Operations:** Complete Create, Read, Update, and Delete functionality for People management.
-   **Advanced Search & Filtering:** Sophisticated search with name, age range, and email domain filters.
-   **Smart Pagination:** Efficient pagination with configurable page sizes and sorting options.
-   **Statistical Analytics:** Age range queries, email domain statistics, and demographic insights.

### 🔐 Security & Authentication
-   **JWT Authentication:** Secure token-based authentication with configurable policies.
-   **Role-Based Authorization:** Granular access control with Admin/User role differentiation.
-   **Input Validation:** Comprehensive validation using FluentValidation with detailed error messages.

### 🏗️ Enterprise Architecture
-   **Service Layer Pattern:** Clean separation of concerns with dedicated business logic layer.
-   **Automatic Database Initialization:** Self-configuring database creation and table setup.
-   **Dual Database Approach:** Entity Framework Core + Custom PostgreSQL handlers for flexibility.
-   **Comprehensive Logging:** Structured logging with Serilog including file output and request tracking.

### 🧪 Quality Assurance
-   **Extensive Test Suite:** 25+ tests covering unit, integration, and performance scenarios.
-   **Performance Testing:** Scalability validation and response time monitoring.
-   **Integration Testing:** End-to-end API testing with real database interactions.
-   **Continuous Validation:** Automated testing pipeline ensuring code quality.

### 🛠️ Developer Experience
-   **Swagger Integration:** Interactive API documentation and testing interface.
-   **Environment Configuration:** Secure `.env` file management for sensitive data.
-   **Docker Ready:** Container-optimized for modern deployment scenarios.
-   **Resilience Patterns:** Polly integration for retry policies and fault tolerance.

---

## 🌐 API Endpoints

### 🔐 Authentication Endpoints
```http
POST /api/auth/register    # Register new user
POST /api/auth/login       # Authenticate and get JWT token
```

### 👥 People Management
```http
# Basic CRUD Operations
GET    /api/people              # Get all people
GET    /api/people/{id}         # Get person by ID
POST   /api/people              # Create new person
PUT    /api/people/{id}         # Update person
DELETE /api/people/{id}         # Delete person (Admin only)

# Advanced Search & Analytics
GET    /api/people/search       # Advanced search with filters
GET    /api/people/age-range    # Get people in age range
GET    /api/people/statistics   # Get demographic statistics
GET    /api/people/count-by-email-domain  # Count by email domain
```

### 🔍 Advanced Search Features

The `/api/people/search` endpoint supports sophisticated filtering:

**Query Parameters:**
- `name` - Filter by name (partial match)
- `minAge` / `maxAge` - Age range filtering
- `emailDomain` - Filter by email domain (e.g., "gmail.com")
- `sortBy` - Sort field (Name, Age, Email, CreatedAt)
- `sortDirection` - Sort direction (Asc, Desc)
- `pageNumber` - Page number (default: 1)
- `pageSize` - Items per page (default: 10)

**Example Request:**
```http
GET /api/people/search?name=John&minAge=25&maxAge=45&emailDomain=gmail.com&sortBy=Age&sortDirection=Desc&pageNumber=1&pageSize=10
```

**Example Response:**
```json
{
  "items": [
    {
      "id": 1,
      "name": "John Doe",
      "age": 35,
      "email": "john@gmail.com"
    }
  ],
  "totalCount": 150,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 15,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

---

## 📸 Demo & Screenshots

Here is a look at the Swagger UI, which makes interacting with the API straightforward.

*(Placeholder: Fügen Sie hier einen Screenshot Ihrer Swagger-UI ein, z.B. `![Swagger UI Demo](link_zum_screenshot.png)`)*

---

## 🚀 Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes.

### Prerequisites

-   [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
-   [PostgreSQL 16+](https://www.postgresql.org/download/) with appropriate user permissions
-   An API testing tool like [Postman](https://www.postman.com/), [Insomnia](https://insomnia.rest/), or `curl`
-   Optional: [Docker](https://www.docker.com/) for containerized deployment

### Installation & Setup

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/coder-gabba/SQLRestCSharp.git
    cd SQLRestCSharp
    ```

2.  **Configure Environment Variables:**
    Create a `.env` file by copying the example file. This is crucial for security.
    ```bash
    cp .env.example .env
    ```
    Now, open the new `.env` file and replace the placeholder values with your actual database and JWT credentials.
    ```ini
    # PostgreSQL Connection String
    SQL_CONNECTION_STRING="Host=localhost;Port=5432;Database=YourDatabase;Username=YourUser;Password=YourPassword"

    # JWT Settings
    JWT_ISSUER="YourApp.com"
    JWT_AUDIENCE="YourAPI.com"
    JWT_KEY="Your_super_secret_key_that_is_very_long_and_at_least_32_chars"
    ```

3.  **Database Setup (Automatic):**
    The application includes automatic database initialization! Simply ensure your PostgreSQL server is running and the user has proper permissions:
    ```bash
    # Grant permissions to your PostgreSQL user (run as postgres user)
    sudo -u postgres psql
    GRANT ALL PRIVILEGES ON DATABASE sqlapi TO your_username;
    GRANT ALL ON SCHEMA public TO your_username;
    GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO your_username;
    \q
    ```
    *Note: The application will automatically create the database and all required tables on first run.*

4.  **Run the Application:**
    ```bash
    dotnet run
    ```
    The API is now running and accessible at `https://localhost:<port>` and `http://localhost:<port>`. The Swagger UI will be available at the root URL (e.g., `https://localhost:7123`).

5.  **Run Tests (Optional):**
    Verify everything is working correctly by running the comprehensive test suite:
    ```bash
    # Run all tests
    dotnet test
    
    # Run specific test categories
    dotnet test --filter "PersonService"        # Service layer tests
    dotnet test --filter "Integration"          # Integration tests
    dotnet test --filter "Performance"          # Performance tests
    ```
    **Expected Result:** All 25+ tests should pass, confirming the application is working correctly.

---

## 🧪 Testing & Quality Assurance

This project includes a comprehensive testing strategy:

### Test Categories
- **Unit Tests (12 tests):** Service layer business logic validation
- **Integration Tests (8 tests):** End-to-end API functionality testing  
- **Performance Tests (8 tests):** Scalability and response time validation
- **Validator Tests (4 tests):** Input validation and error handling

### Running Tests
```bash
# Run all tests with detailed output
dotnet test --verbosity normal

# Run tests with coverage (if coverage tools are installed)
dotnet test --collect:"XPlat Code Coverage"

# Run specific test files
dotnet test --filter "ClassName=PersonServiceTests"
```

### Test Features
- **In-Memory Database:** Tests use isolated in-memory databases for fast execution
- **Mock Dependencies:** External dependencies are mocked for reliable testing
- **Performance Benchmarks:** Automated performance regression detection
- **Comprehensive Assertions:** FluentAssertions for readable test output

---

## 🕹️ API Usage Examples

Here are some examples of how to interact with the API using `curl`.

### 1. Register a new user

```bash
curl -X POST "https://localhost:7123/api/auth/register" \
     -H "Content-Type: application/json" \
     -d '{"username": "testuser", "password": "Password123!", "role": "User"}'
```

### 2. Log in to get a token

```bash
curl -X POST "https://localhost:7123/api/auth/login" \
     -H "Content-Type: application/json" \
     -d '{"username": "testuser", "password": "Password123!"}'
```
**Example Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### 3. Get all people (with token)

```bash
TOKEN="your_jwt_token_here"
curl -X GET "https://localhost:7123/api/people" \
     -H "Authorization: Bearer $TOKEN"
```

### 4. Advanced Search Example

```bash
# Search for people named "John" between ages 25-45 with Gmail addresses
curl -X GET "https://localhost:7123/api/people/search?name=John&minAge=25&maxAge=45&emailDomain=gmail.com&sortBy=Age&sortDirection=Desc&pageNumber=1&pageSize=5" \
     -H "Authorization: Bearer $TOKEN"
```

### 5. Get Age Range Statistics

```bash
# Get all people between ages 20-65
curl -X GET "https://localhost:7123/api/people/age-range?minAge=20&maxAge=65" \
     -H "Authorization: Bearer $TOKEN"
```

### 6. Get Email Domain Statistics

```bash
# Count users by email domain
curl -X GET "https://localhost:7123/api/people/count-by-email-domain?domain=gmail.com" \
     -H "Authorization: Bearer $TOKEN"
```

### 7. Get Demographic Statistics

```bash
# Get comprehensive statistics
curl -X GET "https://localhost:7123/api/people/statistics" \
     -H "Authorization: Bearer $TOKEN"
```

**Example Statistics Response:**
```json
{
  "totalCount": 1250,
  "averageAge": 34.5,
  "ageDistribution": {
    "18-25": 245,
    "26-35": 412,
    "36-45": 338,
    "46-55": 186,
    "56+": 69
  },
  "topEmailDomains": [
    {"domain": "gmail.com", "count": 456},
    {"domain": "yahoo.com", "count": 234},
    {"domain": "outlook.com", "count": 198}
  ]
}
```

---

## 🔧 Troubleshooting & FAQ

**Q: I get a 401 Unauthorized error even with a token.**
**A:** Make sure your token is not expired and that you are sending it in the correct format: `Authorization: Bearer <YourToken>`. Also, verify that the `JWT_KEY`, `JWT_ISSUER`, and `JWT_AUDIENCE` in your `.env` file match the values used to generate the token.

**Q: Database connection fails.**
**A:** The application will automatically create the database if it doesn't exist. Ensure your PostgreSQL server is running and that the user specified in your connection string has the necessary permissions. Check the logs for detailed error information.

**Q: Tests are failing.**
**A:** Run `dotnet clean` followed by `dotnet build` to ensure a clean build. Tests use in-memory databases and should not require external database connectivity. Check that all NuGet packages are restored with `dotnet restore`.

**Q: Performance tests are timing out.**
**A:** Performance test thresholds are set for typical development environments. If running on slower hardware, you can adjust the timeout values in the test files or skip performance tests with `dotnet test --filter "FullyQualifiedName!~Performance"`.

---

## 🔐 Security Best Practices

-   **Never commit your `.env` file** to version control. The `.gitignore` file is already configured to prevent this.
-   Use strong, unique passwords for your database.
-   In a production environment, use a more robust secret management solution like Azure Key Vault, AWS Secrets Manager, or HashiCorp Vault instead of a `.env` file.
-   Regularly rotate your `JWT_KEY`.

---

## ☁️ Deployment

This API is container-ready. You can build a Docker image using a `Dockerfile`.

*(Placeholder)*

The application can be deployed to any cloud provider that supports .NET, such as Azure App Service, AWS Elastic Beanstalk, or Heroku.

---

## 🗺️ Roadmap

### ✅ Completed Features
-   [x] Comprehensive unit and integration tests (25+ tests)
-   [x] Advanced search and filtering capabilities
-   [x] Structured logging with Serilog
-   [x] Service layer architecture implementation
-   [x] Automatic database initialization
-   [x] Performance testing and optimization
-   [x] Statistical analytics and reporting

### 🚀 Upcoming Features
-   [ ] Implement refresh tokens for longer-lived sessions
-   [ ] Docker containerization with multi-stage builds
-   [ ] Redis caching for improved performance
-   [ ] GraphQL endpoint support
-   [ ] Real-time notifications with SignalR
-   [ ] API rate limiting and throttling
-   [ ] Comprehensive audit logging
-   [ ] Export functionality (CSV, Excel, PDF)

---

## 📜 Changelog

**v2.0.0 (Current - Enterprise Edition)**
-   ✨ **New:** Advanced search and filtering with pagination
-   ✨ **New:** Service layer architecture with PersonService
-   ✨ **New:** Comprehensive test suite (25+ tests)
-   ✨ **New:** Performance testing and monitoring
-   ✨ **New:** Statistical analytics endpoints
-   ✨ **New:** Automatic database initialization
-   ✨ **New:** Structured logging with Serilog
-   🔧 **Enhanced:** Error handling with global exception handler
-   � **Enhanced:** Input validation with FluentValidation
-   🔧 **Enhanced:** PostgreSQL integration improvements

**v1.0.0**
-   Initial release
-   Basic CRUD for People, JWT Authentication, Role-based Authorization

---

## 🤝 Contributing

Contributions are welcome! Please fork the repository and open a pull request with your changes.

1.  Fork the Project
2.  Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3.  Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4.  Push to the Branch (`git push origin feature/AmazingFeature`)
5.  Open a Pull Request

---

## 📄 License

This project is licensed under the MIT License. See the `LICENSE` file for details.

---

## 📚 Learn More

-   [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
-   [Entity Framework Core Docs](https://docs.microsoft.com/ef/core/)
-   [About JWT](https://jwt.io/)
