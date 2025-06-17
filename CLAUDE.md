# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a League of Legends statistics dashboard application with a separated frontend/backend architecture:

- **Backend**: C# WPF desktop application (`/backend/`) that interfaces with the Riot Games API
- **Frontend**: Next.js web application (`/frontend/`) that displays player statistics and match data

## Common Development Commands

### Frontend (Next.js)
```bash
cd frontend
pnpm install          # Install dependencies (pnpm is preferred over npm)
pnpm dev             # Start development server
pnpm build           # Build for production
pnpm start           # Start production server
pnpm lint            # Run ESLint (currently disabled during builds)
```

### Backend (.NET WPF)
```bash
cd backend
dotnet restore       # Restore NuGet packages
dotnet build         # Build the application
dotnet run           # Run the application
```

## Code Architecture

### Backend Architecture (`/backend/`)
- **Entry Point**: `App.xaml`/`App.xaml.cs` - WPF application startup
- **Main UI**: `MainWindow.xaml`/`MainWindow.xaml.cs` - Primary application window
- **API Integration**: `RiotApiService.cs` - Handles all Riot Games API calls
- **Data Models**: `Models.cs` - Contains DTOs for API responses (SummonerDto, AccountDto, MatchDto, etc.)
- **Bridge Layer**: `BackendApiBridge.cs` - Communication bridge between backend and frontend
- **Caching**: `PlayerCache.cs` - Caches player data to reduce API calls
- **Analytics**: `PerformanceCalculation.cs` - Calculates game performance metrics

### Frontend Architecture (`/frontend/`)
- **App Router**: Next.js 15 app router in `/app/` directory
- **UI Components**: Shadcn/ui component library in `/components/ui/` (50+ components)
- **Main Dashboard**: `lol-stats-dashboard.tsx` - Primary statistics interface
- **Theming**: `theme-provider.tsx` - Dark/light mode support
- **Charts**: Uses Recharts library for data visualization
- **Styling**: Tailwind CSS with custom configuration

### Key Dependencies

**Backend:**
- `DotNetEnv` (3.1.1) - Environment variable management
- `Microsoft.Web.WebView2` (1.0.2210.55) - Web view integration

**Frontend:**
- Next.js 15.2.4 with React 19
- Extensive Radix UI component library
- React Hook Form for form management
- Zod for validation

## Important Configuration Notes

### Build Configuration
- **Frontend**: ESLint and TypeScript errors are currently ignored during builds (configured in `next.config.mjs`)
- **Backend**: Nullable reference types are enabled, providing better null safety

### Package Management
- **Frontend**: Uses pnpm as primary package manager (prefer over npm/yarn)
- **Frontend**: Also has bun.lock present as alternative

### Environment Setup
- **Backend**: Uses DotNetEnv for environment variable management
- **Frontend**: No environment configuration files currently present

## Testing Status
- **No testing framework is currently configured** for either frontend or backend
- When adding tests, research similar projects to determine appropriate testing approach
- Consider Jest/Vitest for frontend and MSTest/NUnit/xUnit for backend

## Code Quality Tools
- **Frontend**: Next.js ESLint available via `pnpm lint` but disabled during builds
- **Backend**: Basic .NET nullable reference types enabled
- **No automated formatting tools** (Prettier, etc.) currently configured
- **No pre-commit hooks** or CI/CD pipelines configured