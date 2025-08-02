<div align="center">
  <h1 align="center">SqlAPI - REST API with .NET</h1>
  <p align="center">
    A robust REST API built with ASP.NET Core demonstrating CRUD operations, JWT authentication, and role-based authorization with a PostgreSQL database.
  </p>
</div>

<p align="center">
  <img alt=".NET" src="https://img.shields.io/badge/.NET-9-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" />
  <img alt="C#" src="https://img.shields.io/badge/C%23-12-239120?style=for-the-badge&logo=c-sharp&logoColor=white" />
  <img alt="PostgreSQL" src="https://img.shields.io/badge/PostgreSQL-16-4169E1?style=for-the-badge&logo=postgresql&logoColor=white" />
  <img alt="License" src="https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge" />
</p>

---

## ✨ Features

-   **Full CRUD Operations:** Create, Read, Update, and Delete functionality for a `People` entity.
-   **Secure Authentication:** JWT-based authentication to protect endpoints.
-   **Role-Based Authorization:** Differentiated access levels (e.g., `Admin` role for deletion).
-   **PostgreSQL Integration:** Uses Entity Framework Core for seamless database interaction.
-   **Secure Configuration:** Sensitive data like connection strings and JWT secrets are managed via a `.env` file.
-   **API Documentation:** Integrated Swagger UI for easy testing and API exploration.
-   **Clean Architecture:** Organized project structure with clear separation of concerns (`Data`, `Models`, `Services`, `Controllers`).

---

## 🚀 Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes.

### Prerequisites

-   [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
-   [PostgreSQL](https://www.postgresql.org/download/)
-   An API testing tool like [Postman](https://www.postman.com/) or use the built-in Swagger UI.

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

## 🕹️ Usage & API Endpoints

After starting the application, you can use the Swagger UI to interact with the API.

### Authentication (`/api/auth`)

First, you need to register a user and then log in to get a JWT.

1.  **Register a user:** Send a `POST` request to `/api/auth/register`.
2.  **Log in:** Send a `POST` request to `/api/auth/login` with the user's credentials.
3.  **Use the Token:** Copy the received JWT and click the "Authorize" button in Swagger. Paste the token in the format `Bearer <YourToken>`.

| Method | Endpoint        | Description                               |
| :------ | :-------------- | :----------------------------------------- |
| `POST`  | `/register`     | Registers a new user.                      |
| `POST`  | `/login`        | Logs in a user and returns a JWT.          |

### People (`/api/people`)

All endpoints require a valid JWT in the `Authorization` header.

| Method | Endpoint        | Description                               | Required Role |
| :------ | :-------------- | :----------------------------------------- | :-------------- |
| `GET`   | `/`             | Retrieves a list of all people.            | Any             |
| `GET`   | `/{id}`         | Retrieves a single person by their ID.     | Any             |
| `POST`  | `/`             | Creates a new person.                      | Any             |
| `PUT`   | `/{id}`         | Updates an existing person.                | Any             |
| `DELETE`| `/{id}`         | Deletes a person by their ID.              | **Admin**       |

---

## 🤝 Contributing

Contributions are what make the open-source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

1.  Fork the Project
2.  Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3.  Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4.  Push to the Branch (`git push origin feature/AmazingFeature`)
5.  Open a Pull Request

---

## 📚 Learn More

-   [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
-   [Entity Framework Core Docs](https://docs.microsoft.com/ef/core/)
-   [About JWT](https://jwt.io/)
