MessaggiKeycloak

Requisiti
.NET SDK 10
Docker Desktop
Keycloak

powershell
docker run -p 8080:8080 -e KC_BOOTSTRAP_ADMIN_USERNAME=admin -e KC_BOOTSTRAP_ADMIN_PASSWORD=admin quay.io/keycloak/keycloak:26.8.1 start-dev

Su http://localhost:8080 (admin / admin): Create realm → importa realm-export.json
Nel realm realmAPI crea un utente (email, nome, cognome, password non temporanea)

Avvio

powershell
dotnet build

API A:

powershell
dotnet run --no-build --no-launch-profile --urls "http://localhost:5001"

API B:

powershell
$env:ASPNETCORE_ENVIRONMENT="ApiB"
dotnet run --no-build --no-launch-profile --urls "http://localhost:5002"

Test:

powershell
$token = (Invoke-RestMethod -Method Post `
  -Uri "http://localhost:8080/realms/realmAPI/protocol/openid-connect/token" `
  -Body @{ grant_type="password"; client_id="test-client"; username="UTENTE"; password="PASSWORD" }).access_token

Invoke-RestMethod -Method Post -Uri "http://localhost:5001/api/invia" `
  -Headers @{ Authorization = "Bearer $token" } `
  -ContentType "application/json" -Body '{"testo":"ciao"}'

Per il verso B → A usare la porta 5002.