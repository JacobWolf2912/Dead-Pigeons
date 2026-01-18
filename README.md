# Dead Pigeons - Lottery System

## What is this?

Dead Pigeons is a lottery game system for a sports club. Players buy lottery tickets (called "boards") with numbers on them. Each week the admin draws 3 winning numbers. If your board has all 3 numbers, you win money.

## How the game works

- Players buy boards with 5-8 numbers (costs 20-160 DKK depending on how many numbers)
- Numbers are always between 1 and 16
- Each week admin draws 3 winning numbers
- If your board contains all 3 winning numbers, your board wins
- Order doesn't matter - just needs all 3 numbers somewhere on the board
- Money flows like this: Players deposit money -> buy boards -> win prizes if numbers match

Example: You pick numbers 2, 5, 7, 10, 13. Admin draws 2, 5, 7. You win because you have all three.

## Tech Stack

Backend: ASP.NET Core 8.0 (C#)
Database: SQL Server
Frontend: React 18 (TypeScript)
Testing: XUnit with TestContainers

## How to set up

1. Install .NET 8.0 SDK (https://dotnet.microsoft.com/download)
2. Install Node.js (https://nodejs.org/)
3. Install Visual Studio or VS Code
4. Install Docker if you want to run tests

Start the backend:
```
cd "Dead Pigeons"
dotnet restore
dotnet build
dotnet ef database update --project ..\DeadPigeons.Infrastructure
dotnet run
```

This starts the API on http://localhost:5047

Start the frontend:
```
cd frontend
npm install
npm start
```

This starts React on http://localhost:3000

## Folder structure

Dead Pigeons/ - Main backend project
  Controllers/ - Handles HTTP requests
  Program.cs - Startup config

DeadPigeons.Infrastructure/ - Database stuff
  Services/ - Business logic
  Repositories/ - Database queries
  Migrations/ - Database changes over time

DeadPigeons.Core/ - Data models
  Entities/ - Player, Board, Game, Transaction classes

frontend/ - React app
  src/pages/ - DashboardPage, AdminPanel, LoginPage
  src/services/ - Calls to API
  src/context/ - Auth state management

## How it works

The system has two types of users: Players and Admins.

Players can:
- Register (but have to wait for admin approval)
- Log in
- Deposit money using Mobile Pay
- Buy lottery boards
- View their balance and boards

Admins can:
- Approve new player registrations
- Approve/reject deposits (and edit the amount if player made a mistake)
- Draw winning numbers each week
- Manage player accounts (activate/deactivate)

Admin login: admin@deadpigeons.dk / Pa55word.

## Database structure

Players - Basic info (name, email, phone, active status)
AspNetUsers - Login info (email, password hash)
Games - Each week's game
GameWinningNumbers - The 3 winning numbers for each game
Boards - Boards people bought (references Player and Game)
BoardNumbers - Individual numbers on each board
Transactions - Deposits (money coming in)
PendingPlayers - Registrations waiting for approval

## User flow

New player:
1. Goes to /register, fills form, clicks register
2. System creates PendingPlayer (not a real account yet)
3. Player sees "Wait for admin approval"
4. Admin goes to Admin Panel, approves the player
5. Now player can log in
6. Player deposits money via Mobile Pay
7. Admin reviews and approves the deposit
8. Player's balance increases
9. Player buys a board for upcoming week
10. Balance decreases by board price
11. Admin draws numbers for that week
12. System checks if player's board wins

## What's done

- User registration and approval workflow
- Login/logout with passwords
- Player dashboard (view boards and balance)
- Admin panel (manage players, transactions, draw numbers)
- Deposit system (with admin approval)
- Board purchasing with price calculation
- Game/drawing system
- 56 tests covering all the main logic
- Board winning detection

## What's not done yet

- Reset password feature (placeholder exists)
- Can't repeat boards for multiple weeks
- Can't copy boards
- No email notifications
- No mobile optimization
- Not deployed to Azure yet

## How to run tests

```
dotnet test
```

This runs all tests. Tests use TestContainers to spin up a real SQL Server database automatically.

To run a specific test class:
```
dotnet test --filter "PlayerServiceTests"
```

Tests are in DeadPigeons.Test/Services/

## Important files to know

Controllers/AuthController.cs - Handles login, registration, admin stuff
Controllers/TransactionsController.cs - Deposits and approvals
DeadPigeons.Infrastructure/Services/PlayerService.cs - Player logic
DeadPigeons.Infrastructure/Services/BoardService.cs - Board pricing and validation
DeadPigeons.Infrastructure/Services/GameService.cs - Drawing numbers
DeadPigeons.Infrastructure/Services/TransactionService.cs - Balance and deposits
frontend/src/pages/DashboardPage.tsx - Player dashboard
frontend/src/pages/AdminPanel.tsx - Admin stuff

## API endpoints

POST /api/auth/register - Create account (becomes pending)
POST /api/auth/login - Log in
GET /api/auth/me - Get current user

GET /api/auth/admin/pending-players - List pending (admin only)
POST /api/auth/admin/approve-player/{id} - Approve one (admin only)

POST /api/transactions/deposit - Submit a deposit
GET /api/transactions/balance - Get your balance
GET /api/transactions/pending - List pending (admin only)
POST /api/transactions/{id}/approve - Approve deposit (admin only)
POST /api/transactions/{id}/dismiss - Reject deposit (admin only)

POST /api/boards/purchase - Buy a board
GET /api/boards/my-boards - Get your boards

GET /api/games - List all games
POST /api/games/{id}/draw-numbers - Draw numbers (admin only)

## Common problems

Port 5047 already in use:
- Kill whatever is using it or change the port in launchSettings.json

Can't connect to database:
- Check SQL Server is running
- Check connection string in appsettings.json
- Try: dotnet ef database drop && dotnet ef database update

Frontend won't start:
- Delete frontend/node_modules and package-lock.json
- Run npm install again

Tests fail with Docker error:
- Make sure Docker Desktop is running

Login not working:
- Check if account is approved by admin
- Check if password is right
- Check if account is active (not deactivated)
- Look at browser console (F12) or backend terminal for error message

## Security stuff

Passwords are hashed using ASP.NET Identity, so they're not stored as plain text.

Users need to be approved by admin before they can log in. This is done through the PendingPlayers table. When admin approves them, a real account is created.

Admin is different from regular users. Only accounts in the Admin role can access the admin panel.

Each API request needs a valid JWT token (proves you logged in).

## Database migrations

If you change an entity (like add a new property to Player), you need to create a migration:

```
dotnet ef migrations add MigrationName --project ..\DeadPigeons.Infrastructure
dotnet ef database update --project ..\DeadPigeons.Infrastructure
```

This creates a new migration file that describes what changed.

## Testing approach

We have 4 test classes:
- PlayerServiceTests (11 tests)
- BoardServiceTests (11 tests)
- GameServiceTests (13 tests)
- TransactionServiceTests (18 tests)

Each test is either "happy path" (things work correctly) or "unhappy path" (error cases).

Tests use TestDataBuilder helper to create test objects consistently.

## Useful commands

Build everything:
```
dotnet build
```

Run backend:
```
dotnet run
```

Run tests:
```
dotnet test
```

Run specific test:
```
dotnet test --filter "TestName"
```

Start frontend dev server:
```
npm start
```

Build frontend for production:
```
npm run build
```

Check database schema:
```
dotnet ef migrations list --project ..\DeadPigeons.Infrastructure
```

## Next steps to deploy

1. Create Azure account
2. Create Azure SQL Database
3. Push backend to Azure App Service
4. Deploy frontend to Azure Static Web Apps
5. Set up GitHub Actions to auto-deploy

## Questions to ask yourself when reading code

- What does this class/method do?
- Who calls this?
- What happens if the input is invalid?
- Is this tested?
- Could this be simpler?
- Are there any bugs here?

Just trace through the code and follow the flow. If you get stuck, check the tests - they usually show how things are supposed to work.

## Random notes

- All IDs are GUIDs (not numbers)
- Everything is async (uses await)
- Services have the business logic, repositories just talk to the database
- Controllers are pretty thin (just call services)
- Validation happens in services, not in controllers
- Frontend talks to API, doesn't know about database at all
- Most things are tested

That's it. Good luck.
