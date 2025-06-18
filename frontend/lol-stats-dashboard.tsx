"use client"

import { useState, useEffect } from "react"
import { CartesianGrid, Line, LineChart, XAxis, YAxis, ReferenceArea } from "recharts"
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

function MatchChart({ data }: { data: MatchSummary["data"] }) {
  // Calculate the data range to position the gradient correctly
  const allValues = data.flatMap(d => [d.yourImpact || 0, d.teamImpact || 0]);
  const minValue = Math.min(...allValues);
  const maxValue = Math.max(...allValues);
  
  return (
    <ChartContainer config={chartConfig} className="h-[250px] w-full">
      <LineChart
        data={data}
        margin={{
          top: 10,
          left: 10,
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
      const matches = await BackendBridge.getPlayerMatchData(name, tag, 5);
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
        <div className="text-center space-y-2">
          <h1 className="text-4xl font-bold text-white">League of Legends Match History</h1>
          <p className="text-blue-200">Performance Timeline & Impact Analysis</p>
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
          <div className="space-y-6">
            {matchesData.map((match) => (
              <Card key={match.id} className="bg-slate-800/50 border-slate-600/50">
                <CardHeader>
                  <div className="flex justify-between items-start">
                    <div>
                      <CardTitle className="text-white flex items-center gap-3">
                        {match.champion}
                        <Badge variant={match.gameResult === "Victory" ? "default" : "destructive"}>
                          {match.gameResult}
                        </Badge>
                      </CardTitle>
                      <CardDescription className="text-slate-300 mt-1">
                        {match.summonerName} ⏱️ {match.gameTime} ⚔️ KDA: {match.kda}
                      </CardDescription>
                    </div>
                    <div className="text-right space-y-1">
                      <div className="text-slate-300 text-sm">
                        CS: {match.cs} • Vision: { match.visionScore ? match.visionScore : "Feature coming soon" }
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
