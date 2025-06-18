const isDev = process.env.NODE_ENV !== 'production';
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
  ...(isDev && {
    async rewrites() {
      return [
        {
          source: '/api/:path*',
          destination: 'http://localhost:5000/api/:path*',
        },
      ];
    },
  }),
  // In production we emit a fully static site (out/) which will be
  // served by the C# backend directly.
  ...(!isDev && { output: 'export' }),
}

export default nextConfig
