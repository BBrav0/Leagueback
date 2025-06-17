// Bridge utility for communicating with C# backend via WebView2

declare global {
  interface Window {
    chrome: {
      webview: {
        hostObjects: {
          backendBridge: {
            GetAccount(gameName: string, tagLine: string): Promise<string>;
            GetMatchHistory(puuid: string, count?: number): Promise<string>;
            AnalyzeMatchPerformance(matchId: string, userPuuid: string): Promise<string>;
          };
        };
      };
    };
  }
}

export interface AccountData {
  puuid: string;
  gameName: string;
  tagLine: string;
}

export interface ChartDataPoint {
  minute: number;
  yourImpact: number;
  teamImpact: number;
}

export interface MatchSummary {
  id: string;
  summonerName: string;
  champion: string;
  rank: string;
  kda: string;
  cs: number;
  visionScore: number;
  gameResult: "Victory" | "Defeat";
  gameTime: string;
  data: ChartDataPoint[];
}

export interface PerformanceAnalysisResult {
  success: boolean;
  matchSummary?: MatchSummary;
  error?: string;
}

export interface LeagueClientInfo {
  gameName: string;
  tagLine: string;
  isAvailable: boolean;
}

export class BackendBridge {
  private static isWebView2Available(): boolean {
    return typeof window !== 'undefined' && 
           window.chrome && 
           window.chrome.webview && 
           window.chrome.webview.hostObjects && 
           window.chrome.webview.hostObjects.backendBridge;
  }

  static async getAccount(gameName: string, tagLine: string): Promise<AccountData | null> {
    if (!this.isWebView2Available()) {
      console.error('WebView2 bridge is not available');
      return null;
    }

    try {
      const result = await window.chrome.webview.hostObjects.backendBridge.GetAccount(gameName, tagLine);
      const data = JSON.parse(result);
      
      if (data.error) {
        console.error('Backend error:', data.error);
        return null;
      }
      
      return data as AccountData;
    } catch (error) {
      console.error('Error calling GetAccount:', error);
      return null;
    }
  }

  static async getMatchHistory(puuid: string, count: number = 5): Promise<string[] | null> {
    if (!this.isWebView2Available()) {
      console.error('WebView2 bridge is not available');
      return null;
    }

    try {
      const result = await window.chrome.webview.hostObjects.backendBridge.GetMatchHistory(puuid, count);
      const data = JSON.parse(result);
      
      if (data.error) {
        console.error('Backend error:', data.error);
        return null;
      }
      
      return data as string[];
    } catch (error) {
      console.error('Error calling GetMatchHistory:', error);
      return null;
    }
  }

  static async analyzeMatchPerformance(matchId: string, userPuuid: string): Promise<PerformanceAnalysisResult | null> {
    if (!this.isWebView2Available()) {
      console.error('WebView2 bridge is not available');
      return null;
    }

    try {
      const result = await window.chrome.webview.hostObjects.backendBridge.AnalyzeMatchPerformance(matchId, userPuuid);
      const data = JSON.parse(result) as PerformanceAnalysisResult;
      
      if (!data.success) {
        console.error('Backend error:', data.error);
        return data; // Return the error result so frontend can handle it
      }
      
      return data;
    } catch (error) {
      console.error('Error calling AnalyzeMatchPerformance:', error);
      return {
        success: false,
        error: `Communication error: ${error}`
      };
    }
  }

  static async getPlayerMatchData(gameName: string, tagLine: string, matchCount: number = 5): Promise<MatchSummary[]> {
    const matches: MatchSummary[] = [];
    
    try {
      // Get player account
      const account = await this.getAccount(gameName, tagLine);
      if (!account) {
        throw new Error('Failed to get account information');
      }

      // Get match history
      const matchIds = await this.getMatchHistory(account.puuid, matchCount);
      if (!matchIds || matchIds.length === 0) {
        throw new Error('No match history found');
      }

      // Analyze each match
      for (const matchId of matchIds) {
        const analysis = await this.analyzeMatchPerformance(matchId, account.puuid);
        if (analysis && analysis.success && analysis.matchSummary) {
          matches.push(analysis.matchSummary);
        }
      }

      return matches;
    } catch (error) {
      console.error('Error getting player match data:', error);
      throw error;
    }
  }

  static async getLeagueClientInfo(): Promise<LeagueClientInfo> {
    try {
      const response = await fetch('/api/LeagueClient/league-client-info');
      if (!response.ok) {
        throw new Error('Failed to get League client info');
      }
      return await response.json();
    } catch (error) {
      return {
        gameName: '',
        tagLine: '',
        isAvailable: false
      };
    }
  }
}