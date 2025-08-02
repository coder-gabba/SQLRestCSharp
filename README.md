# SqlAPI

Dies ist eine REST-API, die mit ASP.NET Core erstellt wurde und grundlegende CRUD-Operationen (Create, Read, Update, Delete) für eine "Personen"-Entität demonstriert. Die API ist durch JWT (JSON Web Tokens) gesichert und enthält eine rollenbasierte Autorisierung.

## Funktionen

-   **CRUD-Operationen:** Vollständige Erstellung, Lesung, Aktualisierung und Löschung für Personen.
-   **Authentifizierung:** Sicherer Login-Endpunkt, der JWTs generiert.
-   **Autorisierung:** Endpunkte sind geschützt. Bestimmte Aktionen (wie Löschen) erfordern eine "Admin"-Rolle.
-   **Datenbank:** Verwendet Entity Framework Core mit PostgreSQL.
-   **Sichere Konfiguration:** Lädt sensible Daten (Datenbank-Verbindungszeichenfolge, JWT-Schlüssel) aus einer `.env`-Datei.
-   **API-Dokumentation:** Integrierte Swagger-UI mit detaillierten Beschreibungen und XML-Kommentaren.

## Voraussetzungen

-   [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) oder höher
-   [PostgreSQL](https://www.postgresql.org/download/)
-   Ein API-Testtool wie [Postman](https://www.postman.com/) oder die Verwendung der integrierten Swagger-UI.

## Einrichtung

1.  **Repository klonen:**
    ```bash
    git clone https://github.com/coder-gabba/SQLRestCSharp.git
    cd SQLRestCSharp
    ```

2.  **Umgebungsvariablen konfigurieren:**
    Erstellen Sie eine `.env`-Datei im Hauptverzeichnis des Projekts, indem Sie die `.env.example`-Datei kopieren:
    ```bash
    cp .env.example .env
    ```
    Öffnen Sie die neue `.env`-Datei und ersetzen Sie die Platzhalterwerte durch Ihre tatsächlichen Daten:
    ```
    # PostgreSQL Connection String
    SQL_CONNECTION_STRING="Host=localhost;Port=5432;Database=IhreDatenbank;Username=IhrBenutzer;Password=IhrPasswort"

    # JWT Settings
    JWT_ISSUER="IhreAnwendung.com"
    JWT_AUDIENCE="IhreAPI.com"
    JWT_KEY="Ihr_super_geheimer_schlüssel_der_sehr_lang_ist_und_mindestens_32_zeichen_hat"
    ```

3.  **XML-Dokumentation aktivieren (Optional, für bessere Swagger-UI):**
    Um die API-Dokumentation in Swagger zu verbessern, aktivieren Sie die Erstellung der XML-Dokumentationsdatei in den Projekteinstellungen (`SqlAPI.csproj`):
    ```xml
    <PropertyGroup>
      <GenerateDocumentationFile>true</GenerateDocumentationFile>
      <NoWarn>$(NoWarn);1591</NoWarn>
    </PropertyGroup>
    ```

4.  **Datenbankmigrationen anwenden:**
    Diese Befehle erstellen die Datenbank und die Tabellen (`People` und `Users`) basierend auf den Modellen im Code.
    ```bash
    dotnet ef migrations add InitialCreate
    dotnet ef database update
    ```

5.  **Anwendung starten:**
    ```bash
    dotnet run
    ```
    Die API ist jetzt unter `https://localhost:port` und `http://localhost:port` erreichbar. Die Swagger-UI finden Sie direkt unter der Stamm-URL (z.B. `https://localhost:7123`).

## API-Endpunkte

### Authentifizierung (`/api/auth`)

| Methode | Endpunkt        | Beschreibung                               |
| :------ | :-------------- | :----------------------------------------- |
| `POST`  | `/register`     | Registriert einen neuen Benutzer.          |
| `POST`  | `/login`        | Meldet einen Benutzer an und gibt einen JWT zurück. |

### Personen (`/api/people`)

Alle Endpunkte erfordern einen gültigen JWT im `Authorization`-Header (`Bearer <token>`).

| Methode | Endpunkt        | Beschreibung                               | Benötigte Rolle |
| :------ | :-------------- | :----------------------------------------- | :-------------- |
| `GET`   | `/`             | Ruft eine Liste aller Personen ab.         | Beliebig        |
| `GET`   | `/{id}`         | Ruft eine einzelne Person anhand ihrer ID ab. | Beliebig        |
| `POST`  | `/`             | Erstellt eine neue Person.                 | Beliebig        |
| `PUT`   | `/{id}`         | Aktualisiert eine vorhandene Person.       | Beliebig        |
| `DELETE`| `/{id}`         | Löscht eine Person anhand ihrer ID.        | **Admin**       |
