# PocketAdvisor

A personal finance management web app for tracking income, expenses, and transfers across multiple accounts.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+ and npm 11+](https://nodejs.org)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for the database)

---

## 1. Database

Start the PostgreSQL container from the root of the repository:

```bash
docker compose up -d
```

The default credentials are defined in `.env`. The backend connects on `localhost:5432` by default.

---

## 2. Backend

All commands below should be run from the `Backend/PocketAdvisor.WebApplication/` directory.

### 2.1 Install the SecureStore CLI

```bash
dotnet tool install --global SecureStore.Client
```

### 2.2 Create the secrets store

```bash
SecureStore create ./secrets.bin --keyfile ./secrets.key
```

### 2.3 Populate the required secrets

The following secrets are required. The database credentials must match the values in `.env` (or your Docker setup):

```bash
SecureStore --store ./secrets.bin --keyfile ./secrets.key set "ConnectionStrings:DefaultUsername" "<POSTGRES_USER>"
SecureStore --store ./secrets.bin --keyfile ./secrets.key set "ConnectionStrings:DefaultPassword" "<POSTGRES_PASSWORD>"
SecureStore --store ./secrets.bin --keyfile ./secrets.key set "Resend:ApiKey" "<your-resend-api-key>"
SecureStore --store ./secrets.bin --keyfile ./secrets.key set "TokenSecrets:EmailVerification" "<random-secret>"
SecureStore --store ./secrets.bin --keyfile ./secrets.key set "TokenSecrets:JsonWeb" "<random-secret>"
SecureStore --store ./secrets.bin --keyfile ./secrets.key set "TokenSecrets:PasswordReset" "<random-secret>"
SecureStore --store ./secrets.bin --keyfile ./secrets.key set "TokenSecrets:Refresh" "<random-secret>"
```

The host, port, and database name are already configured in `appsettings.Development.json` and match the Docker defaults — no changes needed there unless you deviate from them.

### 2.4 Run the backend

```bash
dotnet run
```

The API starts on `http://localhost:5078`. Migrations are applied automatically on startup. In Development mode, seed data (two users, sample accounts, categories, items, and transactions) is inserted if the database is empty.

**Seed credentials:**

| Role | Email | Password |
|---|---|---|
| Administrator | `admin@pocketadvisor.dev` | `Admin12!` |
| User | `user@pocketadvisor.dev` | `User123!` |

Swagger UI is available at `http://localhost:5078/swagger`.

---

## 3. Frontend

All commands below should be run from the `Frontend/` directory.

### 3.1 Installation dependencies

```bash
npm install
```

### 3.2 Run the development server

```bash
npm start
```

The app is served at `http://localhost:4200` and will proxy API calls to the backend.
