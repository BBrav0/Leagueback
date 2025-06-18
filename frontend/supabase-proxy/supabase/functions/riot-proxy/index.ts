// supabase/functions/riot-proxy/index.ts

// deno-lint-ignore-file no-explicit-any
// @ts-nocheck

import { serve } from "https://deno.land/std@0.168.0/http/server.ts"

// CORS for desktop app & browser
const corsHeaders = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Headers': 'authorization, x-client-info, apikey, content-type',
}

serve(async (req) => {
  // CORS pre-flight
  if (req.method === 'OPTIONS') {
    return new Response('ok', { headers: corsHeaders })
  }

  try {
    const url = new URL(req.url)
    const path = url.searchParams.get('path') // e.g. "/riot/account/v1/..."

    // Secret stored in Supabase → `supabase secrets set RIOT_API_KEY=...`
    const RIOT_API_KEY = Deno.env.get('RIOT_API_KEY')
    if (!RIOT_API_KEY) {
      throw new Error('RIOT_API_KEY is not set in Supabase secrets.')
    }

    // 1. If a path is provided, act as a lightweight proxy to Riot
    if (path) {
      const riotUrl = `https://americas.api.riotgames.com${path}`
      const riotRes = await fetch(riotUrl, {
        headers: { 'X-Riot-Token': RIOT_API_KEY }
      })

      const body = await riotRes.text()
      return new Response(body, {
        status: riotRes.status,
        headers: { ...corsHeaders, 'Content-Type': riotRes.headers.get('content-type') ?? 'application/json' }
      })
    }

    // 2. If no path param, just return the key (used once by the desktop app)
    return new Response(RIOT_API_KEY, { headers: { ...corsHeaders, 'Content-Type': 'text/plain' } })
  } catch (err) {
    return new Response(
      JSON.stringify({ error: err.message }),
      { status: 400, headers: { ...corsHeaders, 'Content-Type': 'application/json' } },
    )
  }
})