# Platform API manual checks

Open `platform-api.http` in an editor with `.http` request support, such as Visual Studio Code with the REST Client extension or JetBrains Rider, then run each request.

The requests target `http://localhost:5000`, matching the Platform API port in `docker/docker-compose.yml`. Start the API with `docker compose -f docker/docker-compose.yml up platform-api` from the repository root, and configure `PLATFORM_DB_CONNECTION` in `docker/platform-api.env` before trying signup. The health check does not require the database; signup does.

The signup requests use a generated GUID in the tenant key and email so you can repeat them without colliding with previous manual runs. The success request should return HTTP 201, and the invalid password request should return HTTP 400.
