"use client"

import { useState, useEffect } from "react"
import { CartesianGrid, Line, LineChart, XAxis, YAxis, ReferenceArea, PieChart, Pie, Cell } from "recharts"
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  ChartLegend,
  ChartLegendContent,
} from "@/components/ui/chart"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { BackendBridge, MatchSummary } from "@/lib/bridge"


const chartConfig = {
  yourImpact: {
    label: "Your Impact",
    color: "#FFFDD0", 
  },
  teamImpact: {
    label: "Team Impact",
    color: "#FDB813", 
  },
}

// Impact overview pie-chart color config
const pieConfig = {
  impactWins: {
    label: "Impact Wins",
    color: "#22c55e", // Green
  },
  impactLosses: {
    label: "Impact Losses",
    color: "#ef4444", // Red
  },
  guaranteedWins: {
    label: "Guaranteed Wins",
    color: "#3b82f6", // Blue
  },
  guaranteedLosses: {
    label: "Guaranteed Losses",
    color: "#fde047", // Yellow
  },
} as const

// Type describing the aggregate counts for each category
type ImpactCounts = {
  impactWins: number
  impactLosses: number
  guaranteedWins: number
  guaranteedLosses: number
}

function ImpactPieChart({ counts }: { counts: ImpactCounts }) {
  // Convert counts → pie chart data on each render
  const pieData: { name: keyof typeof pieConfig; value: number }[] = [
    { name: "impactWins", value: counts.impactWins },
    { name: "impactLosses", value: counts.impactLosses },
    { name: "guaranteedWins", value: counts.guaranteedWins },
    { name: "guaranteedLosses", value: counts.guaranteedLosses },
  ]

  const total = pieData.reduce((acc, cur) => acc + cur.value, 0);

  return (
    <ChartContainer
      config={pieConfig}
      className="h-[300px] w-full justify-center"
    >
      <PieChart>
        <Pie
          data={pieData}
          dataKey="value"
          nameKey="name"
          cx="50%"
          cy="50%"
          innerRadius={60}
          outerRadius={110}
          paddingAngle={2}
          strokeWidth={0}
          label={({ name, value }) => {
            const percent = total > 0 ? ((value as number) / total) * 100 : 0;
            return `${pieConfig[name as keyof typeof pieConfig].label} ${percent.toFixed(0)}%`;
          }}
          labelLine={false}
        >
          {pieData.map((entry) => (
            <Cell
              key={`cell-${entry.name}`}
              fill={pieConfig[entry.name as keyof typeof pieConfig].color}
            />
          ))}
        </Pie>
        <ChartTooltip content={<ChartTooltipContent />} />
        <ChartLegend content={<ChartLegendContent />} />
      </PieChart>
    </ChartContainer>
  )
}

function MatchChart({ data }: { data: MatchSummary["data"] }) {
  // Round values to 2 decimal places for a cleaner chart display
  const roundedData = data.map((d) => ({
    ...d,
    yourImpact: Number(d.yourImpact.toFixed(1)),
    teamImpact: Number(d.teamImpact.toFixed(1)),
  }));

  // Calculate the data range to position the gradient correctly
  const allValues = roundedData.flatMap(d => [d.yourImpact || 0, d.teamImpact || 0]);
  const minValue = Math.min(...allValues);
  const maxValue = Math.max(...allValues);
  
  return (
    <ChartContainer config={chartConfig} className="h-[250px] w-full justify-start">
      <LineChart
        data={roundedData}
        margin={{
          top: 10,
          left: -25,
          right: 10,
          bottom: 10,
        }}
      >
        {/* === START: GRADIENT DEFINITIONS === */}
        <defs>
          {/* Gradient for the POSITIVE (green) area */}
          <linearGradient id="positiveGradient" x1="0" y1="1" x2="0" y2="0">
            {/* Goes from bottom (y1=1) to top (y2=0) */}
            <stop offset="0%" stopColor="rgba(34, 197, 94, 0.3)" /> {/* Subtle Green at y=0 axis */}
            <stop offset="100%" stopColor="rgba(34, 197, 94, 0.9)" /> {/* Rewarding Green at top */}
          </linearGradient>

          {/* Gradient for the NEGATIVE (red) area */}
          <linearGradient id="negativeGradient" x1="0" y1="0" x2="0" y2="1">
            {/* Goes from top (y1=0) to bottom (y2=1) */}
            <stop offset="0%" stopColor="rgba(239, 68, 68, 0.30)" /> {/* Subtle Red at y=0 axis */}
            <stop offset="100%" stopColor="rgba(239, 68, 68, 0.9)" /> {/* Vicious Red at bottom */}
          </linearGradient>
        </defs>
        {/* === END: GRADIENT DEFINITIONS === */}

        {/* Green background for positive values (above 0) */}
        <ReferenceArea
          y1={0}
          y2={maxValue + 10}
          fill="url(#positiveGradient)" // Apply the positive gradient
        />
        {/* Red background for negative values (below 0) */}
        <ReferenceArea
          y1={minValue - 10}
          y2={0}
          fill="url(#negativeGradient)" // Apply the negative gradient
        />

        <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--muted-foreground))" opacity={0.3} />
        <XAxis
          dataKey="minute"
          tickLine={false}
          axisLine={false}
          tickMargin={8}
          tick={{ fill: "hsl(var(--muted-foreground))", fontSize: 12 }}
          tickFormatter={(value) => (value === 35 ? "Final" : `${value}m`)}
        />
        <YAxis
          tickLine={false}
          axisLine={false}
          tickMargin={8}
          tick={{ fill: "hsl(var(--muted-foreground))", fontSize: 12 }}
          domain={["dataMin - 10", "dataMax + 10"]}
        />
        <ChartTooltip
          cursor={{ stroke: "hsl(var(--muted-foreground))", strokeWidth: 1 }}
          content={<ChartTooltipContent labelFormatter={(value) => (value === 35 ? "Final" : `Minute ${value}`)} />}
        />
        <Line
          dataKey="yourImpact"
          type="monotone"
          stroke={chartConfig.yourImpact.color}
          strokeWidth={2}
          dot={{ fill: chartConfig.yourImpact.color, strokeWidth: 1, r: 3 }}
        />
        <Line
          dataKey="teamImpact"
          type="monotone"
          strokeDasharray="5 5"
          stroke={chartConfig.teamImpact.color}
          strokeWidth={2}
          dot={{ fill: chartConfig.teamImpact.color, strokeWidth: 1, r: 3 }}
        />
        <ChartLegend content={<ChartLegendContent />} />
      </LineChart>
    </ChartContainer>
  )
}

export default function Component() {
  const [matchesData, setMatchesData] = useState<MatchSummary[]>([]);
  const [impactCounts, setImpactCounts] = useState<ImpactCounts>({
    impactWins: 0,
    impactLosses: 0,
    guaranteedWins: 0,
    guaranteedLosses: 0,
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [gameName, setGameName] = useState("");
  const [tagLine, setTagLine] = useState("");
  const [hasSearched, setHasSearched] = useState(false);
  const [showManualInput, setShowManualInput] = useState(false);

  useEffect(() => {
    const checkLeagueClient = async () => {
      const clientInfo = await BackendBridge.getLeagueClientInfo();
      if (clientInfo.isAvailable) {
        setGameName(clientInfo.gameName);
        setTagLine(clientInfo.tagLine);
        setShowManualInput(false);
        handleSearchWithInfo(clientInfo.gameName, clientInfo.tagLine);
      } else {
        setShowManualInput(true);
      }
    };

    checkLeagueClient();
  }, []);

  // Periodically re-check if League client becomes available
  useEffect(() => {
    if (!showManualInput) return;
    const interval = setInterval(async () => {
      const clientInfo = await BackendBridge.getLeagueClientInfo();
      if (clientInfo.isAvailable) {
        setGameName(clientInfo.gameName);
        setTagLine(clientInfo.tagLine);
        setShowManualInput(false);
        handleSearchWithInfo(clientInfo.gameName, clientInfo.tagLine);
      }
    }, 5000); // Check every 5 seconds
    return () => clearInterval(interval);
  }, [showManualInput]);

  const handleSearchWithInfo = async (name: string, tag: string) => {
    if (!name || !tag) {
      setError("Please enter both game name and tag line");
      return;
    }

    setLoading(true);
    setError(null);
    setHasSearched(true);

    try {
      const matches = await BackendBridge.getPlayerMatchData(name, tag, 10);

      // Calculate impact category counts
      const counts: ImpactCounts = {
        impactWins: 0,
        impactLosses: 0,
        guaranteedWins: 0,
        guaranteedLosses: 0,
      };

      matches.forEach((m) => {
        const youHigher = m.yourImpact > m.teamImpact;
        const win = m.gameResult === "Victory";
        if (win && youHigher) counts.impactWins++;
        else if (!win && !youHigher) counts.impactLosses++;
        else if (!win && youHigher) counts.guaranteedLosses++;
        else if (win && !youHigher) counts.guaranteedWins++;
      });

      setImpactCounts(counts);
      setMatchesData(matches);
      if (matches.length === 0) {
        setError("No matches found for this player");
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to fetch match data");
      setMatchesData([]);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = () => {
    handleSearchWithInfo(gameName, tagLine);
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleSearch();
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-950 via-purple-900 to-blue-900 p-6">
      <div className="max-w-6xl mx-auto space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          {/* Title & subtitle */}
          <div className="space-y-2">
            <h1 className="text-4xl font-bold text-white">League of Legends Match History</h1>
            <p className="text-blue-200">Performance Timeline & Impact Analysis</p>
          </div>

          {/* Placeholder action buttons */}
          <div className="flex gap-2">
            <Button variant="secondary">Settings</Button>
            <Button variant="secondary">Account</Button>
          </div>
        </div>

        {/* Search Form - Only show if manual input is needed */}
        {showManualInput && (
          <Card className="bg-slate-800/50 border-slate-600/50">
            <CardHeader>
              <CardTitle className="text-white">Enter Summoner Information</CardTitle>
              <CardDescription className="text-slate-300">
                League client not detected. Please launch the client or enter your Riot ID manually
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="flex gap-4 items-end">
                <div className="flex-1">
                  <Label htmlFor="gameName" className="text-white">Game Name</Label>
                  <Input
                    id="gameName"
                    value={gameName}
                    onChange={(e) => setGameName(e.target.value)}
                    onKeyPress={handleKeyPress}
                    placeholder="Enter game name"
                    className="bg-slate-700 border-slate-600 text-white placeholder-slate-400"
                  />
                </div>
                <div className="flex-1">
                  <Label htmlFor="tagLine" className="text-white">Tag Line</Label>
                  <Input
                    id="tagLine"
                    value={tagLine}
                    onChange={(e) => setTagLine(e.target.value)}
                    onKeyPress={handleKeyPress}
                    placeholder="Enter tag line (e.g., NA1)"
                    className="bg-slate-700 border-slate-600 text-white placeholder-slate-400"
                  />
                </div>
                <Button 
                  onClick={handleSearch} 
                  disabled={loading || !gameName || !tagLine}
                  className="px-8"
                >
                  {loading ? "Loading..." : "Analyze"}
                </Button>
              </div>
            </CardContent>
          </Card>
        )}

        {/* Error Display */}
        {error && (
          <Alert className="bg-red-900/50 border-red-600">
            <AlertDescription className="text-red-200">
              {error}
            </AlertDescription>
          </Alert>
        )}

        {/* Loading State */}
        {loading && (
          <Card className="bg-slate-800/50 border-slate-600/50">
            <CardContent className="p-8 text-center">
              <div className="text-white text-lg">Analyzing matches...</div>
              <div className="text-slate-300 text-sm mt-2">This may take a few moments</div>
            </CardContent>
          </Card>
        )}

        {/* Match List */}
        {hasSearched && !loading && matchesData.length > 0 && (
          <div className="flex flex-col md:flex-row gap-6">
            {/* Match cards */}
            <div className="md:w-3/5 space-y-6">
              {matchesData.map((match) => (
                <Card key={match.id} className="bg-slate-800/50 border-slate-600/50 w-full">
                  <CardHeader>
                    <div className="flex justify-between items-start">
                      <div>
                        <CardTitle className="text-white flex items-center gap-3">
                          {match.champion}
                          <Badge
                            variant={match.gameResult === "Victory" ? "default" : "destructive"}
                            className={match.gameResult === "Victory" ? "bg-green-600 text-white hover:bg-green-600" : ""}
                          >
                            {match.gameResult}
                          </Badge>
                        </CardTitle>
                        <CardDescription className="text-slate-300 mt-1">
                          {match.summonerName} ⏱️ {match.gameTime} ⚔️ {match.kda}  <br />
                          🧙 {match.cs} 🔎 {match.visionScore}
                        </CardDescription>
                      </div>
                      <div className="text-right space-y-1">
                        <div className="text-slate-300 text-sm">
                          Your Average Score: {match.yourImpact.toFixed(2)} <br />
                          Average Teammate Score: { match.teamImpact.toFixed(2) }
                        </div>
                        <div className="text-slate-400 text-xs">
                          {match.rank}
                        </div>
                      </div>
                    </div>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-4">
                      <div className="text-slate-300 text-sm font-medium">Performance Timeline</div>
                      <MatchChart data={match.data} />
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>

            {/* Impact overview pie chart */}
            <div className="md:flex-1">
              <Card className="bg-slate-800/50 border-slate-600/50 h-[450px] flex flex-col sticky top-6">
                <CardHeader>
                  <CardTitle className="text-white">Impact Overview</CardTitle>
                  <CardDescription className="text-slate-300">Placeholder distribution</CardDescription>
                </CardHeader>
                <CardContent className="flex-1 flex items-center justify-center">
                  <ImpactPieChart counts={impactCounts} />
                </CardContent>
              </Card>
            </div>
          </div>
        )}

        {/* No Data State */}
        {hasSearched && !loading && matchesData.length === 0 && !error && (
          <Card className="bg-slate-800/50 border-slate-600/50">
            <CardContent className="p-8 text-center">
              <div className="text-slate-300 text-lg">No match data found</div>
              <div className="text-slate-400 text-sm mt-2">Try a different summoner name or check your spelling</div>
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  )
}
