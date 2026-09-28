# PocketAdvisor

A personal finance management web app for tracking income, expenses, and transfers across multiple accounts.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+ and npm 11+](https://nodejs.org)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for the database and, optionally, the containerized backend and frontend)

---

## 1. Database

All commands in this section are run from the root of the repository.

### 1.1 Create the environment file

Docker Compose reads its settings from a `.env` file, which is excluded via `.gitignore`. Create it from the committed template and replace the placeholder values:

```bash
cp .env.example .env
```

The comments in `.env.example` describe every variable and which other settings they have to match.

### 1.2 Start the database

```bash
docker compose up -d --wait db
```

`--wait` returns once the database health check passes. Only the `db` service is started here; `docker compose up` without a service name would also start the containerized backend and frontend (see [2.5](#25-run-the-backend-in-docker-alternative-to-24) and [3.3](#33-run-the-frontend-in-docker-alternative-to-32)).

The backend connects on `localhost:5432` by default. The port is published on the loopback interface (`127.0.0.1`) only, so the database is not reachable from other machines on the network.

---

## 2. Backend

All commands below should be run from the `Backend/PocketAdvisor.WebApplication/` directory.

### 2.1 Install the SecureStore CLI

```bash
dotnet tool install --global SecureStore.Client
```

### 2.2 Obtain the key file (existing store)

The encrypted secrets store (`secrets.bin`) is committed to the repository. Only the decryption key (`secrets.key`) is excluded via `.gitignore`.

If you are joining the existing project, ask the project owner for `secrets.key` through a secure out-of-band channel. Place it next to `secrets.bin` in this directory. The store is then ready to use, so skip to [2.4](#24-run-the-backend).

> **Never commit `secrets.key`.** If the key is ever exposed, rotate every secret in the store. Old ciphertext remains in git history and can be decrypted with the leaked key.

### 2.3 Create your own store (independent setup only)

Follow this step only if you don't have access to the shared key, for example on a fork or a fully independent environment. Creating a new store **overwrites** the committed `secrets.bin`, so do not commit the result back to the shared repository.

```bash
SecureStore create ./secrets.bin --keyfile ./secrets.key
```

Then populate the required secrets. The database credentials must match the values in `.env` (or your Docker setup):

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

### 2.5 Run the backend in Docker (alternative to 2.4)

The backend can also run as a container next to the database. The image is built from `Backend/Dockerfile` and contains no secrets: `secrets.bin` and `secrets.key` (see [2.2](#22-obtain-the-key-file-existing-store) / [2.3](#23-create-your-own-store-independent-setup-only)) are mounted into the container at runtime as Compose secrets, so the key file has to be in place before the first start.

Stop `dotnet run` first, because both use host port `5078`. Then, from the root of the repository:

```bash
docker compose up -d --build --wait api
docker compose ps
curl -i http://localhost:5078/health
```

`--wait` returns once the database and the API report healthy. The API container only starts after the database is healthy, then applies the migrations and, in Development, the seed data exactly as in 2.4.

The following variables in `.env` affect the backend container (all optional):

| Variable | Default | Purpose |
|---|---|---|
| `APP_VERSION` | `development` | Image tag (`pocketadvisor-api:<version>`, `pocketadvisor-web:<version>`) and the version recorded in the images |
| `API_PORT` | `5078` | Host port of the API, published on `127.0.0.1` only (the container listens on `8080`). The frontend image picks it up automatically; for `npm start`, update `API_URL` of the development configuration in `Frontend/angular.json` as well. |
| `ASPNETCORE_ENVIRONMENT` | `Production` | `.env.example` sets `Development` for local work, which enables Swagger, the data seeding (with the publicly known seed credentials) and detailed error messages. Never use it for a deployment. |

The container reaches the database as `db:5432` on the Compose network, and its health check calls `GET /health` from inside the container. The API runs as the non-root `app` user (UID `1654`); on a Linux host, `secrets.key` must be readable by that user, since Compose secrets are bind mounts.

To stop the containers without losing the database data (this also stops the frontend container, if it runs):

```bash
docker compose down
```

The data is kept in the `db_data` named volume. `docker compose down -v` would delete it as well.

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

The app is served at `http://localhost:4200` and calls the backend at `http://localhost:5078/api`.

The API base URL is not hard-coded in the source: `src/environments/environment.ts` reads the `API_URL` constant, which the `define` option of each build configuration in `angular.json` replaces at build time. The development configuration (`npm start`) targets `http://localhost:5078/api`, the production configuration (`npm run build`) targets `https://api.pocketadvisor.codecameleon.com/api`.

### 3.3 Run the frontend in Docker (alternative to 3.2)

The frontend can also run as a container. The image is built from `Frontend/Dockerfile` in two stages: Node.js installs the locked dependencies (`npm ci`) and builds the production bundle, then only the static files are copied into an unprivileged nginx image, so neither Node.js nor the source code or `node_modules` end up in the runtime image.

Stop `npm start` first, because both use host port `4200`. Then, from the root of the repository, start the whole stack (database, API and frontend):

```bash
docker compose up -d --build --wait
docker compose ps
curl -i http://localhost:4200/health
```

The app is then available at `http://localhost:4200`. The frontend container does not depend on the API container: nginx only serves static files, and it is the browser that calls the API. `docker compose up -d --build --wait web` therefore starts the frontend alone, next to a backend started with `dotnet run`.

The following variables in `.env` affect the frontend container (all optional):

| Variable | Default | Purpose |
|---|---|---|
| `WEB_PORT` | `4200` | Host port of the frontend, published on `127.0.0.1` only (the container listens on `8080`). It must match `Frontend:BaseUrl` of the backend, otherwise CORS rejects the API calls. |
| `API_URL` | `http://localhost:${API_PORT}/api` | The API base URL, without a trailing slash. It is compiled into the bundle, so changing it requires a rebuild (`--build`). |

The API only accepts calls from the configured frontend origin (`Frontend:BaseUrl`), which is `http://localhost:4200` in `appsettings.Development.json`. The containerized frontend therefore works against a backend running in the `Development` environment, as set in `.env.example`.

nginx serves every unknown path with `index.html`, so reloading a deep link such as `/accounts` still loads the app, while a missing `.js` or `.css` file is a real `404`. `index.html` is revalidated on every request and the hashed bundle files are cached for a year, so a new image is picked up on the next page load. Every response carries security headers, including a Content-Security-Policy that only allows scripts from the app itself and jsDelivr (Chart.js), and network calls only to the app and the API.
