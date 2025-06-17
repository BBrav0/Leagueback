/** @type {import('next').NextConfig} */
const nextConfig = {
  eslint: {
    ignoreDuringBuilds: true,
  },
  typescript: {
    ignoreBuildErrors: true,
  },
  images: {
    unoptimized: true,
  },
  async rewrites() {
    return [
      {
        source: '/api/LeagueClient/:path*',
        destination: 'http://localhost:5000/api/LeagueClient/:path*', // Proxy to your backend
      },
    ];
  },
}

export default nextConfig
