<div align="center">
  <h1 align="center">SqlAPI - REST API with .NET</h1>
  <p align="center">
    A robust REST API built with ASP.NET Core demonstrating CRUD operations, JWT authentication, and role-based authorization with a PostgreSQL database.
  </p>
</div>

<p align="center">
  <img alt="Project Status" src="https://img.shields.io/badge/status-active-brightgreen.svg?style=for-the-badge" />
  <img alt=".NET" src="https://img.shields.io/badge/.NET-9-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" />
  <img alt="C#" src="https://img.shields.io/badge/C%23-12-239120?style=for-the-badge&logo=c-sharp&logoColor=white" />
  <img alt="PostgreSQL" src="https://img.shields.io/badge/PostgreSQL-16-4169E1?style=for-the-badge&logo=postgresql&logoColor=white" />
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
-   **Services:** Contain the core business logic (e.g., JWT generation).
-   **Data:** Manages database interactions via Entity Framework Core (`ApplicationDbContext`).
-   **Models:** Define the data structures (`User`, `Person`, `LoginRequest`).

---

## ✨ Features

-   **Full CRUD Operations:** Create, Read, Update, and Delete functionality for a `People` entity.
-   **Secure Authentication:** JWT-based authentication to protect endpoints.
-   **Role-Based Authorization:** Differentiated access levels (e.g., `Admin` role for deletion).
-   **PostgreSQL Integration:** Uses Entity Framework Core for seamless database interaction.
-   **Secure Configuration:** Sensitive data is managed via a `.env` file.
-   **API Documentation:** Integrated Swagger UI for easy testing and API exploration.

---

## 📸 Demo & Screenshots

Here is a look at the Swagger UI, which makes interacting with the API straightforward.

*(Placeholder: Fügen Sie hier einen Screenshot Ihrer Swagger-UI ein, z.B. `![Swagger UI Demo](link_zum_screenshot.png)`)*

---

## 🚀 Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes.

### Prerequisites

-   [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
-   [PostgreSQL](https://www.postgresql.org/download/)
-   An API testing tool like [Postman](https://www.postman.com/) or `curl`.

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

3.  **Apply Database Migrations:**
    These commands will create the database and the necessary tables (`People` and `Users`) based on the code models.
    ```bash
    dotnet ef database update
    ```
    *Note: If you are starting from scratch, you might need to create an initial migration first with `dotnet ef migrations add InitialCreate`.*

4.  **Run the Application:**
    ```bash
    dotnet run
    ```
    The API is now running and accessible at `https://localhost:<port>` and `http://localhost:<port>`. The Swagger UI will be available at the root URL (e.g., `https://localhost:7123`).

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

---

## 🔧 Troubleshooting & FAQ

**Q: I get a 401 Unauthorized error even with a token.**
**A:** Make sure your token is not expired and that you are sending it in the correct format: `Authorization: Bearer <YourToken>`. Also, verify that the `JWT_KEY`, `JWT_ISSUER`, and `JWT_AUDIENCE` in your `.env` file match the values used to generate the token.

**Q: `dotnet ef database update` fails.**
**A:** Ensure your PostgreSQL server is running and that the connection string in your `.env` file is correct.

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

-   [ ] Implement refresh tokens for longer-lived sessions.
-   [ ] Add more comprehensive unit and integration tests.
-   [ ] Introduce a logging framework like Serilog for structured logging.

---

## 📜 Changelog

**v1.0.0 (Current)**
-   Initial release.
-   Features: CRUD for People, JWT Authentication, Role-based Authorization.

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
