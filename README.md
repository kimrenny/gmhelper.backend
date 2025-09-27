# GMHelper Backend

This is the backend of the GMHelper application, built with .NET 9 Web API.

## Table of Contents

- [Overview](#overview)
- [Technologies](#technologies)
- [Environments](#environment-variables)
- [Getting Started](#getting-started)
  - [Running with Docker](#running-with-docker)
  - [Running without Docker](#running-without-docker)
  - [HTTPS / Secure Mode](#https--secure-mode)
- [Running Tests](#running-tests)
- [API Documentation](#api-documentation)
- [Notes](#notes)

## Overview

The backend provides all API endpoints for the GMHelper application, including user authentication, data management, and admin functionalities.

## Technologies

- .NET 9 Web API
- Swagger for API documentation
- Docker for containerized deployment
- xUnit for unit & integration testing

## Environment Variables

For the application to work correctly, create a `.env` file in the root folder of the backend repository with the following content:

```bash
SMTP__Host=
SMTP_Port=
SMTP__Username=
SMTP__Password=
SMTP__From=
CAPTCHA_SecretKey=
ConnectionStrings__DefaultConnection=
ConnectionStrings__DevConnection=

#Development/Production mode
ASPNETCORE_ENVIRONMENT=

# PostgreSQL
POSTGRES_USER=
POSTGRES_PASSWORD=
POSTGRES_DB=

# JWT
JWT_SECRET_KEY=
JWT_ISSUER=
JWT_AUDIENCE=

#CORS
CORS_ORIGINS=
```

> Fill in the values according to your environment. This is required for the backend to run correctly.

## Test Environment

For running integration and unit tests, create a .env.test file in the same folder. Its structure should be identical to .env, but you **SHOULD** use a separate database and different credentials if needed:

> ⚠️ Important: Make sure the database in .env.test is different from your development or production database. Running tests will delete and recreate the test database.

```bash
SMTP__Host=
SMTP_Port=
SMTP__Username=
SMTP__Password=
SMTP__From=
CAPTCHA_SecretKey=
ConnectionStrings__DefaultConnection= # use a dedicated test database!
ConnectionStrings__DevConnection=

#Development/Production mode
ASPNETCORE_ENVIRONMENT=

# PostgreSQL
POSTGRES_USER=
POSTGRES_PASSWORD=
POSTGRES_DB=

# JWT
JWT_SECRET_KEY=
JWT_ISSUER=
JWT_AUDIENCE=

#CORS
CORS_ORIGINS=
```

> This ensures tests run in an isolated environment without affecting your development or production database.

## Getting Started

### Running with Docker

To run the full application (frontend + backend) using Docker:

1. Clone both repositories into the same folder:

   - GMHelper UI: [https://github.com/kimrenny/gmhelper.ui](https://github.com/kimrenny/gmhelper.ui)
   - GMHelper Backend: [https://github.com/kimrenny/gmhelper.backend](https://github.com/kimrenny/gmhelper.backend)

2. Open a terminal in the `backend` folder.

3. Run the following command to build and start containers:

```bash
docker compose up --build
```

4. After the containers are running, the application will be available at:

- Frontend: [http://localhost:4200](http://localhost:4200)
- Backend/API: [http://localhost:7057](http://localhost:7057)

You can also run only the backend using Docker (replace with the exact command if needed).

### Running without Docker

To run the backend locally without Docker:

1. Open a terminal in the `backend` folder.

2. Run the application:

```bash
dotnet watch
```

3. The backend will be available at [http://localhost:7057](http://localhost:7057)

Frontend can be run separately (see README in the [frontend repository](https://github.com/kimrenny/gmhelper.ui)).

### HTTPS / Secure Mode

For proper operation, the backend must be run in HTTPS mode.

To run the backend locally in HTTPS, use one of the following commands

```bash
dotnet run
```

or

```bash
dotnet run --launch-profile https
```

Make sure the frontend is also configured to use HTTPS to avoid mixed content issues.

## Running Tests

To execute all backend tests:

1. Open a terminal in the `backend` folder.

2. Run the following command:

```bash
dotnet test
```

- Tests are written using xUnit.
- The test runner will automatically pick up `.env.test` for configuration.
- Integration tests will create and reset a dedicated test database based on the connection string in `.env.test`.

## API Documentation

Swagger UI is available after running the backend. Open the following link to explore all API endpoints, request/response models, and examples:

[http://localhost:7057/swagger/index.html](http://localhost:7057/swagger/index.html)

## Notes

- Make sure Docker and Docker Compose are installed if running with containers.
- All API endpoints are fully documented in Swagger; no additional endpoint documentation is needed in this README.
- Code comments are included only for non-obvious logic. Method and variable names are self-explanatory.
