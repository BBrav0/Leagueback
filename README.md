# Leagueback

> Quantify your personal impact in every League of Legends match.

Leagueback crunches in-game statistics to tell you—clearly and objectively—whether you carried, inted, or did everything humanly possible. Stop staring at raw KDA; start understanding your real contribution.

## How It Works

In Solo Queue roughly **40 % of matches** are effectively predetermined, while only **20 %** hinge on your individual performance. Leagueback analyses each game and classifies it into one of four outcomes:

| Outcome          | Meaning                                                     |
| ---------------- | ----------------------------------------------------------- |
| Impact Win       | Your play tipped the scales and secured the victory.        |
| Guaranteed Win   | Your team would have won with or without you.               |
| Impact Loss      | Your mistakes directly cost your team the game.            |
| Guaranteed Loss  | Even Faker couldn't have saved this one.                    |

The app surfaces these insights through clean charts and dashboards so you can focus your practice where it matters most.

## Features

- 📊 **Performance dashboard** contrasting you vs. team averages
- 💾 **Local match cache** for ultra-fast history look-ups

## Coming Soon

- ⚡ **Real-time impact score** updated during the match
- 🗺️ **Objective & lane weighting** for an even smarter algorithm
- 🎨 **Customisable themes & settings panel**

## Screenshots

*Coming soon!*

## Installation

1. Download the latest **Leagueback.zip** from the [releases page](https://github.com/your-org/Leagueback/releases).
2. Extract the archive to a folder of your choice.
3. Double-click `Leagueback.exe`—that's it!

### Build From Source

#### Prerequisites

- .NET 8 SDK
- Node 18+ and PNPM (or your preferred package manager)
- Bun (optional but recommended for frontend development)

```bash
# Clone the repository
$ git clone https://github.com/BBrav0/Leagueback.git
$ cd Leagueback

# --- Backend ---
$ cd backend
$ dotnet run

# --- Frontend (in a second terminal) ---
$ cd ../frontend
$ pnpm install
$ pnpm dev
```

## Roadmap

- [ ] Integrate turrets, objectives, and lane data into the algorithm
- [ ] Add pie chart summarising total impact wins & losses
- [ ] Rank icons, scoreboard, and additional UI polish
- [ ] Algorithm smoothing for fairer score curves
- [ ] Settings panel: clear cache, theme customisation, and more

## Contributing

Pull requests are welcome! Feel free to open an issue for feature requests, bug reports, or general discussion.

## Disclaimer

Leagueback isn't endorsed by Riot Games and doesn't reflect the views or opinions of Riot Games or anyone officially involved in producing or managing League of Legends. All in-game content, imagery, and names are registered trademarks of Riot Games, Inc.