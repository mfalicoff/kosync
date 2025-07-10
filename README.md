# kosync-dotnet

**kosync-dotnet** is a self-hostable implementation of the KOReader sync server built with .NET. It aims to extend the existing functionality of the official [koreader-sync-server](https://github.com/koreader/koreader-sync-server).

Users of KOReader can register a user on this synchronisation server and use the inbuilt _Progress sync_ plugin to keep all reading progress synchronised between devices.

## Database Support

The server supports **two database backends**:

- **MongoDB** - Full-featured document database with advanced querying capabilities
- **SQLite** - Lightweight, file-based database for simpler deployments

You can switch between databases by changing the `DatabaseProvider` configuration. Both databases use the same entities and provide identical functionality through a unified repository interface.
## Configuration

Set the database provider in your `appsettings.json`:

```json
{
  "DatabaseProvider": "MongoDB",  // or "SQLite"
  "MongoDB": {
    "ConnectionString": "mongodb://admin:adminpassword@mongodb:27017/KosyncDb?authSource=admin",
    "DatabaseName": "KosyncDb",
    "CollectionName": "KosyncUsers",
    "AdminPassword": "adminpassword"
  },
  "SQLite": {
    "ConnectionString": "Data Source=kosync.db",
    "AdminPassword": "adminpassword"
  }
}
```

## How to run your own server?

### Option 1: MongoDB (Docker Compose)

For full-featured deployments with advanced querying capabilities:

```yaml
services:
  mongodb:
    image: mongo:8.0
    container_name: kosync-mongodb
    restart: unless-stopped
    ports:
      - "27017:27017"
    environment:
      MONGO_INITDB_ROOT_USERNAME: admin
      MONGO_INITDB_ROOT_PASSWORD: adminpassword
      MONGO_INITDB_DATABASE: KosyncDb
    volumes:
      - mongodb_data:/data/db
      - mongodb_config:/data/configdb
    networks:
      - kosync-network

  mongoku:
    image: huggingface/mongoku:latest
    container_name: kosync-mongoku
    restart: unless-stopped
    ports:
      - "3100:3100"
    environment:
      MONGOKU_DEFAULT_HOST: "mongodb://admin:adminpassword@mongodb:27017"
    depends_on:
      - mongodb
    networks:
      - kosync-network

  kosync-app:
    build: .
    image: kosync:latest
    container_name: kosync-application
    restart: unless-stopped
    ports:
      - "5000:8080"
    environment:
      DatabaseProvider: "MongoDB"
      MongoDB__ConnectionString: "mongodb://admin:adminpassword@mongodb:27017/KosyncDb?authSource=admin"
      MongoDB__DatabaseName: "KosyncDb"
      MongoDB__AdminPassword: "adminpassword"
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: "http://+:8080"
    depends_on:
      - mongodb
    networks:
      - kosync-network

networks:
  kosync-network:
    driver: bridge

volumes:
  mongodb_data:
    driver: local
  mongodb_config:
    driver: local
```

### Option 2: SQLite (Standalone)

For simpler deployments without external database dependencies:

```yaml
services:
  kosync-app:
    build: .
    image: kosync:latest
    container_name: kosync-application
    restart: unless-stopped
    ports:
      - "5000:8080"
    environment:
      DatabaseProvider: "SQLite"
      SQLite__ConnectionString: "Data Source=/app/data/kosync.db"
      SQLite__AdminPassword: "adminpassword"
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: "http://+:8080"
    volumes:
      - kosync_data:/app/data

volumes:
  kosync_data:
    driver: local
```

The SQLite database file will be stored in `/app/data/kosync.db` and contains all user and document progress information.

## Environment Variables

**Database Configuration:**
- `DatabaseProvider` - Set to `"MongoDB"` or `"SQLite"` (default: `"MongoDB"`)

**Admin User:**
An admin user with the username `admin` will be created when the server first starts. The admin password is configured via:
- **MongoDB**: `MongoDB__AdminPassword` environment variable
- **SQLite**: `SQLite__AdminPassword` environment variable

If not provided, the password defaults to `admin`. The recommendation is to set this to a strong password generated with a password manager like [Bitwarden](https://bitwarden.com/). This admin user can be used to interact with the management API.

**Other Options:**

- `REGISTRATION_DISABLED` - Set to `"true"` to disable user registration (useful for public deployments)
- `TRUSTED_PROXIES` - Comma-separated list of trusted proxy IP addresses for logging
- `SINGLE_LINE_LOGGING` - Set to `"true"` for single-line log output
- `ASPNETCORE_HTTP_PORTS` - Configure custom HTTP ports (default: `8080`)

## Deployment Notes

- The sync server is accessible via port `8080` inside the container
- The recommendation is to expose the server via a reverse proxy such as [Caddy](https://nginxproxymanager.com/)
- For SQLite deployments, ensure the data directory is properly mounted as a volume
- For MongoDB deployments, consider using the mongoku web interface for database management

## Management API

There are some management API endpoints you can interact with using a tool like [Postman](https://caddyserver.com/).

Only the admin user can make requests to these API endpoints, with the exception of users being allowed to query and delete their own documents and delete their own account.

All requests to these API endpoints require the following headers.

```json
{
  "x-auth-user": "admin"
  "x-auth-key": "<MD5 hash of your admin password>"
}
```

Since we reuse the existing user structure used for KOReader, unfortunately we are stuck using MD5 hashes for passwords instead of something more secure.

### GET /manage/users

Returns a list of all users.

**Example Response**

```json
[
    {
        "id": 1,
        "username": "admin",
        "isAdministrator": true,
        "isActive": true,
        "documentCount": 0
    },
    {
        "id": 1,
        "username": "jberlyn",
        "isAdministrator": false,
        "isActive": true,
        "documentCount": 1
    }
]
```

### POST /manage/users

Creates a new user. This endpoint circumvents the `REGISTRATION_DISABLED` environment variable.

**Request Body**

```json
{
  "username": "jberlyn",
  "password": "super-strong-password"
}
```

**Example Response**

```json
{
  "message": "User created successfully"
}
```

### DELETE /manage/users?username=username

Deletes a user. The username to be deleted must be passed via a query parameter.

**Example Response**

```json
{
    "message": "Success"
}
```

### GET /manage/users/documents?username=username

Returns a list of documents for a user and their sync status. The username for the user must be passed via a query parameter.

**Example Response**

```json
[
    {
        "documentHash": "b8a9c5b494e7c91ece4cb8407e746ec7",
        "progress": "/body/DocFragment[28]/body/div/section/h1[1]/a/span/text().0",
        "percentage": 0.3186,
        "device": "Kobo_nova",
        "deviceId": "F47D5CA7123A4ABEAB90E8BF6F836356",
        "timestamp": "2023-05-23T11:43:00.225+10:00"
    },
    {
        "documentHash": "b1e5b67d6b0fe57ce9893d08dce65406",
        "progress": "/body/DocFragment[10]/body/div/h1/text().0",
        "percentage": 0.0166,
        "device": "Kobo_nova",
        "deviceId": "F47D5CA7123A4ABEAB90E8BF6F836356",
        "timestamp": "2023-05-23T11:44:34.165+10:00"
    }
]
```

### DELETE /manage/users/documents?username=username&documentHash=documentHash

Deletes a document for a user. The username for the user and the document hash must be passed via query parameters.

***Example Response***

```json
{
    "message": "Success"
}
```

### PUT /manage/users/active?username=username

Toggles the active status of a user. The username for the user must be passed via a query parameter. Users that are marked as inactive will not be able to login or push sync progress.

**Example Response**

```json
{
    "message": "User marked as inactive"
}
```

### PUT /manage/users/password?username=username

Updates the password for a user. The username for the user must be passed via a query parameter.

**Request Body**

```json
{
  "password": "super-strong-password"
}
```

**Example Response**

```json
{
    "message": "Password changed successfully"
}
```