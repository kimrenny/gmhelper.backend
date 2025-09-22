# GMHelper Backend

This is the backend of the GMHelper application, built with .NET 8 Web API.

## Table of Contents
- [Overview](#overview)
- [Technologies](#technologies)
- [Getting Started](#getting-started)
  - [Running with Docker](#running-with-docker)
  - [Running without Docker](#running-without-docker)
- [API Documentation](#api-documentation)
- [Notes](#notes)

## Overview
The backend provides all API endpoints for the GMHelper application, including user authentication, data management, and admin functionalities.

## Technologies
- .NET 8 Web API
- Swagger for API documentation
- Docker for containerized deployment

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

## API Documentation
Swagger UI is available after running the backend. Open the following link to explore all API endpoints, request/response models, and examples:

[http://localhost:7057/swagger/index.html](http://localhost:7057/swagger/index.html)

## Notes
- Make sure Docker and Docker Compose are installed if running with containers.
- All API endpoints are fully documented in Swagger; no additional endpoint documentation is needed in this README.
- Code comments are included only for non-obvious logic. Method and variable names are self-explanatory.
