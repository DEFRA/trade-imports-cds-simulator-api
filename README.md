# trade-imports-cds-simulator-api

A CDS (Customs Declarations Service) Simulator API that receives ALVS clearance decision and error XML notifications from HMRC and stores them in MongoDB for retrieval.

* [API Endpoints](#api-endpoints)
* [Authentication](#authentication)
* [Docker Compose](#docker-compose)
* [MongoDB](#mongodb)
* [Inspect MongoDB](#inspect-mongodb)
* [Testing](#testing)
* [Running](#running)
* [Dependabot](#dependabot)

## API Endpoints

### Inbound Notifications

#### `POST ws/CDS/defra/alvsclearanceinbound/v1`

Receives inbound XML notifications from ALVS. The XML body is parsed to determine whether it is a decision or error notification and stored in the corresponding MongoDB collection.

- **Authentication:** None (public)
- **Content-Type:** XML
- **Responses:**
  - `202 Accepted` — notification stored successfully
  - `400 Bad Request` — XML could not be recognised as a decision or error notification

### Decision Notifications

#### `GET decision-notifications`

Retrieves stored decision notifications with optional filtering.

- **Authentication:** Basic Auth with `read` scope
- **Query Parameters:**

  | Parameter | Type       | Required | Description |
  |-----------|------------|----------|-------------|
  | `Mrn`     | `string`   | No       | Filter by Movement Reference Number |
  | `From`    | `DateTime` | No       | Start of date range (must be UTC) |
  | `To`      | `DateTime` | No       | End of date range (must be UTC) |

  At least one parameter must be provided. `From` must be less than or equal to `To`.

- **Response:** `200 OK` with a JSON array of [Notification](#notification-model) objects, or `400 Bad Request` on validation failure.

### Error Notifications

#### `GET error-notifications`

Retrieves stored error notifications. Same query parameters, validation rules, and response format as [Decision Notifications](#decision-notifications).

- **Authentication:** Basic Auth with `read` scope

### Health Checks

| Endpoint             | Authentication | Description |
|----------------------|----------------|-------------|
| `GET /health`            | None       | Basic readiness check |
| `GET /health/authorized` | Required   | Authenticated readiness check |
| `GET /health/all`        | None       | Detailed check including MongoDB status |

### Notification Model

```json
{
  "id": "string",
  "timestamp": "2024-01-01T00:00:00Z",
  "mrn": "string",
  "xml": "string"
}
```

| Field       | Type       | Description |
|-------------|------------|-------------|
| `id`        | `string`   | MongoDB document ID |
| `timestamp` | `DateTime` | UTC timestamp when the notification was stored |
| `mrn`       | `string`   | Movement Reference Number |
| `xml`       | `string`   | HTML-decoded XML content of the notification |

## Authentication

The API uses HTTP Basic Authentication (`Authorization: Basic <base64(clientId:secret)>`).

Clients and their scopes are configured in `appsettings.json` under the `Acl` section:

```json
{
  "Acl": {
    "Clients": {
      "ClientName": {
        "Secret": "client-secret",
        "Scopes": ["read", "write"]
      }
    }
  }
}
```

**Scopes:**

| Scope   | Grants access to |
|---------|------------------|
| `read`  | `GET decision-notifications`, `GET error-notifications` |
| `write` | Reserved for future use |


## Docker Compose

A Docker Compose template is in [compose.yml](compose.yml).

A local environment with:

- This service
- WireMock (for stubbing external APIs)
- MongoDB (replica set)

```bash
docker compose up --build -d
```

A more extensive setup is available in [github.com/DEFRA/cdp-local-environment](https://github.com/DEFRA/cdp-local-environment)

## MongoDB

### MongoDB via Docker

See above.

```
docker compose up -d mongodb
```

### MongoDB locally

Alternatively install MongoDB locally:

- Install [MongoDB](https://www.mongodb.com/docs/manual/tutorial/#installation) on your local machine
- Start MongoDB:
```bash
sudo mongod --dbpath ~/mongodb-cdp
```

### MongoDB in CDP environments

In CDP environments a MongoDB instance is already set up
and the credentials exposed as environment variables.


## Inspect MongoDB

To inspect the Database and Collections locally:
```bash
mongosh
```

You can use the CDP Terminal to access the environments' MongoDB.

## Testing

Run the tests with:

Tests run by running a full `WebApplication` backed by [Ephemeral MongoDB](https://github.com/asimmon/ephemeral-mongo).
Tests do not use mocking of any sort and read and write from the in-memory database.

```bash
dotnet test
````

## Running

Run the application:
```bash
dotnet run --project src/CdsSimulator --launch-profile CdsSimulator
```

## SonarCloud

Example SonarCloud configuration are available in the GitHub Action workflows.

## Dependabot

We have added an example dependabot configuration file to the repository. You can enable it by renaming
the [.github/example.dependabot.yml](.github/example.dependabot.yml) to `.github/dependabot.yml`


## About the licence

The Open Government Licence (OGL) was developed by the Controller of Her Majesty's Stationery Office (HMSO) to enable
information providers in the public sector to license the use and re-use of their information under a common open
licence.

It is designed to encourage use and re-use of information freely and flexibly, with only a few conditions.
