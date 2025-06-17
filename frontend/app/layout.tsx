import type { Metadata } from 'next'
import './globals.css'

export const metadata: Metadata = {
  title: 'Leagueback App',
  description: 'Created by Ben Bravo',
}

const DevelopmentBanner = () => {
  // You can customize the styles and text here
  const bannerStyle: React.CSSProperties = {
    backgroundColor: '#ef4444', // A nice, modern red color
    color: 'white',
    padding: '10px',
    textAlign: 'center',
    fontWeight: 'bold',
    fontSize: '14px',
    width: '100%',
  };

  return (
    <div style={bannerStyle}>
      VERSION 0.2;
      ALGORITHM IS STILL EXTREMELY WEAK;
      UI IS STILL EXTREMELY MINIMAL
    </div>
  );
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode
}>) {
  return (
    <html lang="en">
      <body>

      <DevelopmentBanner />


        {children}

      </body>
    </html>
  )
}
