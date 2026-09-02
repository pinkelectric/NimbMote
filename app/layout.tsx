import type { Metadata } from 'next';
import { Geist } from 'next/font/google';
import './globals.css';

const geist = Geist({ variable: '--font-geist', subsets: ['latin', 'cyrillic'] });

export const metadata: Metadata = {
  title: 'NimbMote — Windows media, on your phone',
  description: 'A private Android remote for Windows media, volume and power controls on your local Wi-Fi.',
  openGraph: {
    title: 'NimbMote — Windows media, on your phone',
    description: 'A private Android remote for Windows media, volume and power controls on your local Wi-Fi.',
    images: ['/og.png'],
  },
  twitter: {
    card: 'summary_large_image',
    title: 'NimbMote — Windows media, on your phone',
    description: 'A private Android remote for Windows media, volume and power controls on your local Wi-Fi.',
    images: ['/og.png'],
  },
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="en"><body className={geist.variable}>{children}</body></html>;
}
