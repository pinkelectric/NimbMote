import type { NextConfig } from 'next';

// The Cloudflare deployment keeps its Worker build. A static export is used by
// the production host so the public download site is reachable independently of
// Cloudflare. The site has no server data or API routes, so both outputs render
// the same public pages.
const nextConfig: NextConfig = process.env.NIMBMOTE_STATIC_EXPORT === '1'
  ? { output: 'export', trailingSlash: true }
  : {};

export default nextConfig;
