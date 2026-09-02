import type { Metadata } from 'next';
import { Geist } from 'next/font/google';
import './globals.css';

const geist = Geist({ variable: '--font-geist', subsets: ['latin', 'cyrillic'] });

export const metadata: Metadata = {
  metadataBase: new URL('https://nimbmote.pinkelectric.workers.dev'),
  title: 'NimbMote — Windows media, on your phone',
  description: 'A private Android remote for Windows media, volume and power controls on your local Wi-Fi.',
  icons: {
    icon: [{ url: '/favicon.svg', type: 'image/svg+xml' }],
    shortcut: ['/favicon.svg'],
  },
  openGraph: {
    type: 'website',
    siteName: 'NimbMote',
    title: 'NimbMote — Windows media, on your phone',
    description: 'A private Android remote for Windows media, volume and power controls on your local Wi-Fi.',
    images: [{ url: '/og-nimbmote.png', width: 2400, height: 1260, alt: 'NimbMote — Windows media, on your phone' }],
  },
  twitter: {
    card: 'summary_large_image',
    title: 'NimbMote — Windows media, on your phone',
    description: 'A private Android remote for Windows media, volume and power controls on your local Wi-Fi.',
    images: ['/og-nimbmote.png'],
  },
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="en"><body className={geist.variable}>{children}</body></html>;
}
