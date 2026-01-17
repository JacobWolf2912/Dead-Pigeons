# Dead Pigeons Lottery - Frontend (React)

This is the React + TypeScript frontend for the Dead Pigeons Lottery application.

## Quick Start

1. Navigate to the frontend directory:
```bash
cd frontend
```

2. Start the development server:
```bash
npm start
```

The application will open at `http://localhost:3000`

**Important:** Ensure the ASP.NET Core backend is running at `https://localhost:5001` for the API calls to work.

## Project Structure

```
frontend/
├── src/
│   ├── pages/           # Page components (Login, Register, Dashboard, PurchaseBoard, AdminPanel)
│   ├── components/      # Reusable components (ProtectedRoute)
│   ├── context/         # React Context for state management (AuthContext)
│   ├── services/        # API service (api.ts) for backend communication
│   ├── types/           # TypeScript type definitions
│   ├── App.tsx          # Main app component with routing
│   └── index.tsx        # Entry point
└── package.json         # Dependencies (react-router-dom, axios)
```

## Available Routes

- `/login` - User authentication
- `/register` - New user registration
- `/dashboard` - Main player dashboard
- `/purchase-board` - Board purchasing interface
- `/admin` - Admin panel (transaction approval and game drawing)

## Features Implemented

### Authentication System
- User registration (email, password, full name, phone)
- User login with JWT token
- Automatic token persistence and restoration
- Protected routes requiring authentication

### Player Features
- **Dashboard**: View balance, boards, games
- **Board Purchase**: Select games, choose 5-8 numbers from 1-16, purchase with balance deduction
- **Balance Display**: Real-time balance from backend
- **Transaction History**: View all transactions
- **Game Listing**: Browse active and closed games

### Admin Features
- **Transaction Approval**: Review and approve pending deposits
- **Draw Numbers**: Draw 3 winning numbers (1-16) for games
- **Game Management**: View all games and their winning results

## Technology Stack

- **React 18** - UI framework
- **TypeScript** - Type safety
- **React Router v6** - Client-side routing
- **Axios** - HTTP client for API calls
- **CSS3** - Styling with responsive design

## API Integration

All API calls go through `src/services/api.ts`:

```typescript
// Example usage
import { gameService, boardService } from '../services/api';

const games = await gameService.getAllGames();
const board = await boardService.purchaseBoard(playerId, gameId, fieldCount, numbers);
```

The API automatically includes the authentication token in headers for protected endpoints.

## Component Architecture

### ProtectedRoute
Wraps authenticated pages to ensure only logged-in users can access them.

### AuthContext
Global state management for:
- User authentication state
- Login/register/logout operations
- Token management
- Loading and error states

## Styling

Each page has its own CSS file with:
- Gradient backgrounds (purple/blue theme)
- Responsive grid layouts
- Form styling and validation
- Button states and animations
- Mobile-first responsive design

## Running the Application

### Development Mode
```bash
npm start
```
Runs at `http://localhost:3000` with hot reload

### Production Build
```bash
npm run build
```
Creates optimized production build in `build/` folder

### Testing
```bash
npm test
```
Runs the test suite

## Testing the Full Flow

1. **Register**: `/register`
2. **Login**: `/login`
3. **Create test game** (backend): `POST /api/games/create-test-game`
4. **Approve deposit** (admin): Visit `/admin`, approve pending transaction
5. **Purchase board**: Click "Purchase Board" on dashboard
6. **Draw numbers** (admin): Visit `/admin`, draw winning numbers

## Key Files

| File | Purpose |
|------|---------|
| `App.tsx` | Main app with routes |
| `context/AuthContext.tsx` | Authentication state management |
| `services/api.ts` | Backend API communication |
| `pages/LoginPage.tsx` | Login form |
| `pages/RegisterPage.tsx` | Registration form |
| `pages/DashboardPage.tsx` | Main player dashboard |
| `pages/PurchaseBoardPage.tsx` | Board purchase interface |
| `pages/AdminPanel.tsx` | Admin transaction and game management |
| `components/ProtectedRoute.tsx` | Route protection wrapper |
| `types/index.ts` | TypeScript type definitions |

## Error Handling

The application includes error handling for:
- Network failures
- Invalid input
- Authentication errors
- API validation errors
- User feedback with error messages

## Notes for Developers

- All API calls are async/await
- Component state uses React hooks (useState, useEffect)
- Authentication token stored in localStorage
- Protected routes redirect unauthenticated users to login
- Form validation before submission
- Loading states for all async operations

## Browser Support

Works on modern browsers:
- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+

## Next Steps for Production

1. Remove `[AllowAnonymous]` attributes from backend endpoints
2. Implement proper role-based authorization
3. Add password reset functionality
4. Set up environment variables for API URL
5. Configure CORS properly
6. Deploy frontend to hosting service
7. Set up HTTPS certificates
8. Add analytics and monitoring

## Troubleshooting

**Connection refused errors**
- Check ASP.NET Core backend is running at https://localhost:5001

**Login fails**
- Verify user credentials
- Check backend database is set up
- Look at browser console for errors

**"Player ID not found"**
- Make sure backend created a Player record for the user
- May need to call `/api/players/create-from-user/{userId}` first

**CORS errors**
- Verify backend allows http://localhost:3000
- Check backend CORS configuration

## Support

For issues, check:
1. Browser console (F12) for error messages
2. Network tab to see API responses
3. Backend logs for server-side errors
4. Ensure backend is running and database is set up

