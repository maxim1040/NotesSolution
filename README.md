# 📒 NotesSolution

Een cross-platform **notitie-app** gebouwd met **.NET MAUI (net8.0)** en een **ASP.NET Core Web API**.  
De app ondersteunt **registratie, login met Identity + JWT**, notities CRUD, offline opslag met SQLite en automatische synchronisatie zodra er verbinding is.  

## 🚀 Functionaliteiten

- ✅ Registratie & login via **ASP.NET Identity**
- ✅ JWT **access/refresh tokens**
- ✅ CRUD notities (create, read, update, delete)
- ✅ Offline ondersteuning met **SQLite**
- ✅ **SyncService**: synchroniseert lokaal en online
- ✅ Automatisch her-aanmelden met **SecureStorage**
- ✅ Alleen notities van de ingelogde gebruiker zichtbaar
- ✅ **Logout** en sessiebeheer
- ✅ Moderne en eenvoudige UI, geoptimaliseerd voor Android

## 🛠️ Technologieën

- **Frontend:** .NET MAUI (XAML)
- **Backend:** ASP.NET Core 8 Web API
- **Database API:** SQL Server (LocalDB)
- **Database App:** SQLite
- **Auth:** Identity Framework, JWT (access + refresh)
- **Tools:** Swagger UI, Visual Studio 2022, Android Emulator

## 📂 Projectstructuur

```
NotesSolution/
│
├── Notes.Api/           # ASP.NET Web API (Identity + NotesController)
│   ├── Controllers/
│   ├── Data/
│   ├── Models/
│   ├── Services/
│   └── Program.cs
│
├── Notes.App/           # .NET MAUI frontend
│   ├── Views/           # XAML pages (Login, Notes, NoteDetail)
│   ├── ViewModels/      # MVVM logic (LoginViewModel, NotesViewModel, …)
│   ├── Services/        # ApiClient, AuthService, LocalDb, SyncService
│   └── MauiProgram.cs
│
└── README.md
```

## ⚙️ Installatie & Setup

### API starten
1. Open solution in **Visual Studio 2022**
2. Stel **Notes.Api** in als startup project
3. Controleer `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=NotesApiDb;Trusted_Connection=True;TrustServerCertificate=True"
   },
   "Jwt": {
     "Key": "30dbc6cb628bef5bc6d414509192935e",
     "Issuer": "NotesApi",
     "Audience": "NotesApp",
     "ExpiresMinutes": 60,
     "RefreshExpiresDays": 7
   }
   ```
4. Run → API beschikbaar op `https://localhost:7019/swagger`

### App starten
1. Stel **Notes.App** in als startup project
2. Selecteer een **Android emulator** (bv. Pixel 5 – API 34)
3. Run → App opent login-scherm
4. API-basisadres in de app:  
   ```csharp
   public const string ApiBase = "https://10.0.2.2:7019"; // emulator → localhost
   ```

## 🔑 Accounts testen
- **Register**: voer email + wachtwoord in → nieuw account
- **Login**: gebruik je email + wachtwoord → token opgeslagen
- **Herstart app** → direct ingelogd door SecureStorage
- **Logout** → tokens verwijderd → terug naar login

## 📝 Notities testen
- **Create** → "Create new note" → titel & inhoud invullen → Save
- **Read** → lijst toont alleen jouw titels
- **Update** → open note → pas tekst aan → Save
- **Delete** → klik prullenbak → note verdwijnt
- **Offline** → flight mode → note toevoegen → lokaal opgeslagen in SQLite
- **Online** → flight mode uit → sync naar API

## 🔒 GDPR & Privacy
- **Data-minimalisatie**: enkel e-mail + notities
- **Export**: gebruiker kan alle notities opvragen via `/api/account/export`
- **Delete**: gebruiker kan zijn account + notities verwijderen via `/api/account` (DELETE)
- **Tokens**: veilig opgeslagen in SecureStorage
- **HTTPS**: in productie vereist

## ✅ Checklist (vereisten)
- ✔ MAUI (.NET 8, XAML frontend)
- ✔ Functionaliteit projectvoorstel (notitie-app)
- ✔ REST API integratie
- ✔ Identity Framework modellen
- ✔ Auto-heraanmelden na login
- ✔ SQLite offline + sync online
- ✔ Async code (await, geen blocking calls)
- ✔ Documentatie aanwezig
- ✔ GDPR in acht genomen
- ✔ Logische UI (Android-friendly)
- ✔ Goede programmeercultuur (MVVM, DI, naming, binding)

---

### 👨‍💻 Auteur
Sabak Maxim – **NotesSolution** – 2025
