"use client"

import { CartesianGrid, Line, LineChart, XAxis, YAxis } from "recharts"
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  ChartLegend,
  ChartLegendContent,
} from "@/components/ui/chart"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"

const matchesData = [
  {
    id: 1,
    summonerName: "RiftMaster2024",
    champion: "Azir the Emperor",
    rank: "Gold II",
    kda: "12/3/8",
    cs: 245,
    visionScore: 32,
    gameResult: "Victory",
    gameTime: "34:12",
    data: [
      { minute: 1, yourImpact: -5, teamImpact: 2 },
      { minute: 5, yourImpact: 15, teamImpact: -8 },
      { minute: 10, yourImpact: 25, teamImpact: 12 },
      { minute: 14, yourImpact: 35, teamImpact: 18 },
      { minute: 20, yourImpact: 45, teamImpact: 28 },
      { minute: 30, yourImpact: 65, teamImpact: 42 },
      { minute: 35, yourImpact: 78, teamImpact: 55 },
    ],
  },
  {
    id: 2,
    summonerName: "ShadowStrike99",
    champion: "Zed the Master of Shadows",
    rank: "Gold II",
    kda: "8/7/4",
    cs: 198,
    visionScore: 18,
    gameResult: "Defeat",
    gameTime: "28:45",
    data: [
      { minute: 1, yourImpact: 3, teamImpact: -2 },
      { minute: 5, yourImpact: -12, teamImpact: -15 },
      { minute: 10, yourImpact: -8, teamImpact: -22 },
      { minute: 14, yourImpact: 5, teamImpact: -18 },
      { minute: 20, yourImpact: 12, teamImpact: -25 },
      { minute: 30, yourImpact: -5, teamImpact: -35 },
      { minute: 35, yourImpact: -15, teamImpact: -42 },
    ],
  },
  {
    id: 3,
    summonerName: "FrostGuardian",
    champion: "Sejuani the Winter's Wrath",
    rank: "Gold II",
    kda: "2/4/18",
    cs: 156,
    visionScore: 45,
    gameResult: "Victory",
    gameTime: "41:23",
    data: [
      { minute: 1, yourImpact: -8, teamImpact: -3 },
      { minute: 5, yourImpact: -5, teamImpact: 8 },
      { minute: 10, yourImpact: 12, teamImpact: 22 },
      { minute: 14, yourImpact: 28, teamImpact: 35 },
      { minute: 20, yourImpact: 42, teamImpact: 48 },
      { minute: 30, yourImpact: 58, teamImpact: 62 },
      { minute: 35, yourImpact: 72, teamImpact: 78 },
    ],
  },
  {
    id: 4,
    summonerName: "ArcaneMystic",
    champion: "Syndra the Dark Sovereign",
    rank: "Gold II",
    kda: "15/2/6",
    cs: 287,
    visionScore: 28,
    gameResult: "Victory",
    gameTime: "26:18",
    data: [
      { minute: 1, yourImpact: 8, teamImpact: 5 },
      { minute: 5, yourImpact: 22, teamImpact: 12 },
      { minute: 10, yourImpact: 45, teamImpact: 25 },
      { minute: 14, yourImpact: 62, teamImpact: 38 },
      { minute: 20, yourImpact: 85, teamImpact: 52 },
      { minute: 30, yourImpact: 95, teamImpact: 68 },
      { minute: 35, yourImpact: 98, teamImpact: 72 },
    ],
  },
]

const chartConfig = {
  yourImpact: {
    label: "Your Impact",
    color: "#22c55e", // Green
  },
  teamImpact: {
    label: "Team Impact",
    color: "#ef4444", // Red
  },
}

function MatchChart({ data }: { data: (typeof matchesData)[0]["data"] }) {
  return (
    <ChartContainer config={chartConfig} className="h-[200px] w-full">
      <LineChart
        data={data}
        margin={{
          top: 10,
          left: 10,
          right: 10,
          bottom: 10,
        }}
      >
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
  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-950 via-purple-900 to-blue-900 p-6">
      <div className="max-w-7xl mx-auto space-y-6">
        {/* Header */}
        <div className="text-center space-y-2">
          <h1 className="text-4xl font-bold text-white">League of Legends Match History</h1>
          <p className="text-blue-200">Performance Timeline & Impact Analysis</p>
        </div>

        {/* Match List */}
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
                    
                  </div>
                  <div className="text-right space-y-1">
                    
                    
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  
                  <MatchChart data={match.data} />
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    </div>
  )
}
