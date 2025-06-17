import type { Metadata } from 'next'
import './globals.css'

export const metadata: Metadata = {
  title: 'Leagueback App',
  description: 'Created by Ben Bravo',
  generator: '',
}

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode
}>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  )
}
