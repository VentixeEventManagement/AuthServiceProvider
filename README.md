# AccountProvider

AccountProvider är en mikrotjänst byggd i .NET för att hantera användarkonton. Den tillhandahåller API:er för att skapa, uppdatera, hämta och ta bort användare. Tjänsten är uppdelad i flera lager för ökad separation av ansvar och enkel testbarhet.

## Innehåll

- [Arkitektur](#arkitektur)
- [Installation](#installation)
- [Exempel på användning](#exempel-på-användning)
- [Kommandon](#kommandon)
- [Testning](#testning)

## Arkitektur

Tjänsten har ett Presentation lager och filerna är uppdelad i en bra mappstruktur. 


## Installation

1. Klona detta repo:

```bash
git clone https://github.com/ditt-användarnamn/AccountProvider.git
cd AccountProvider
```

2. Uppdatera `appsettings.json` med din databasanslutning i `AccountProvider.API`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=AccountProviderDb;Trusted_Connection=True;"
  }
}
```

3. Kör migreringar:

```bash
cd AccountProvider.API
dotnet ef database update
```

4. Starta API:

```bash
dotnet run
```

## Exempel på användning

### Skapa en användare

**POST** `/api/users`

```json
{
  "name": "Test User",
  "email": "test.user@domain.com"
}
```

### Hämta alla användare

**GET** `/api/users`

### Uppdatera användare

**PUT** `/api/users/{id}`

```json
{
  "name": "Test User.",
  "email": "test.user@domain.com"
}
```

### Ta bort användare

**DELETE** `/api/users/{id}`

## Kommandon

| Kommando                        | Beskrivning                        |
|--------------------------------|-------------------------------------|
| `Update-Database`    | Applicerar senaste migreringar till databasen |
| `Add-Migration`| Skapar en ny migrering                              |

## Testning

Enhetstester finns i projektet `AccountProvider.Tests`. Testerna körs med xUnit och Moq.

```bash
cd AccountProvider.Tests
dotnet test
```

## Aktivitetsdiagram

![AuthServiceProvider_Aktivitetsdiagram](https://github.com/user-attachments/assets/4b49e4db-9346-4dd8-982e-84eff8b100c6)

## Sekvensdiagram 

![Sekvensdiagram_AuthServiceProvider](https://github.com/user-attachments/assets/587b265c-d016-4ee7-968b-d50393a0f114)
