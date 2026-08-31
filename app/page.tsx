import {
  ArrowDownToLine,
  Check,
  ChevronRight,
  Download,
  GitBranch,
  LockKeyhole,
  MonitorSmartphone,
  Monitor,
  Music2,
  ShieldCheck,
  Smartphone,
  Volume2,
  Wifi,
} from 'lucide-react';

import { Button } from '@/components/ui/button';

const windowsDownload =
  'https://raw.githubusercontent.com/pinkelectric/Deskora/main/public/downloads/Deskora-Setup-v0.7.6.exe';
const androidDownload =
  'https://raw.githubusercontent.com/pinkelectric/Deskora/main/public/downloads/Deskora-v0.7.4-test.apk';

const features = [
  { icon: Music2, title: 'Media, where you need it', text: 'Play, pause, skip, seek and see the artwork from your Windows media session.' },
  { icon: Volume2, title: 'Windows volume on your phone', text: 'Adjust the computer volume or mute it without reaching for the keyboard.' },
  { icon: Monitor, title: 'Your desktop at a glance', text: 'See the current wallpaper, connection state and power controls in one card.' },
  { icon: ShieldCheck, title: 'Paired on your network', text: 'Deskora connects your phone and PC over the same Wi-Fi with a six-digit pairing code.' },
];

export default function Home() {
  return (
    <main>
      <nav className="site-nav">
        <a className="brand" href="#top" aria-label="Deskora home"><span className="brand-mark" aria-hidden="true"><span /><span /></span><span>Deskora</span></a>
        <div className="nav-links" aria-label="Page navigation"><a href="#how-it-works">How it works</a><a href="#features">Features</a><a href="#downloads">Download</a></div>
        <a className="nav-github" href="#github"><GitBranch size={16} /> GitHub soon</a>
      </nav>

      <section className="hero" id="top">
        <div className="hero-copy">
          <p className="eyebrow"><span /> Windows + Android</p>
          <h1>Your Windows media,<br /><em>right in your hand.</em></h1>
          <p className="hero-text">Deskora turns your Android phone into a calm, private remote for the Windows PC already in your room.</p>
          <div className="hero-actions">
            <Button className="primary-action" render={<a href="#downloads" />}>Get Deskora <ArrowDownToLine /></Button>
            <a className="text-action" href="#how-it-works">See how it works <ChevronRight /></a>
          </div>
          <div className="hero-proof"><span><Check /> Local network only</span><span><Check /> No account</span><span><Check /> Free test build</span></div>
        </div>
        <div className="hero-visual" aria-label="Deskora Android app screenshot">
          <div className="glow glow-violet" /><div className="glow glow-coral" />
          <div className="phone-shell"><img src="/screenshots/deskora-main.jpg" alt="Deskora showing a connected Dell computer and media controls" /></div>
          <div className="signal-card signal-top"><Wifi /> Secure local pairing</div><div className="signal-card signal-bottom"><Volume2 /> Windows volume</div>
        </div>
      </section>

      <section className="section section-muted" id="how-it-works">
        <div className="section-heading centered"><p className="eyebrow"><span /> Three small steps</p><h2>It is a remote, not another cloud account.</h2><p>Both devices stay on the same Wi-Fi. The agent shows a code; the phone uses it to pair.</p></div>
        <div className="steps">
          <article><span className="step-number">01</span><MonitorSmartphone /><h3>Install the Windows Agent</h3><p>Install Deskora Agent on the computer you want to control. On first launch it shows a six-digit code.</p></article>
          <article><span className="step-number">02</span><Smartphone /><h3>Install Deskora on Android</h3><p>Open the app on your phone. The current Android package is a test build until the Google Play release.</p></article>
          <article><span className="step-number">03</span><LockKeyhole /><h3>Enter the code once</h3><p>Enter the code while both devices share Wi-Fi. Deskora remembers the protected pairing afterwards.</p></article>
        </div>
      </section>

      <section className="section features" id="features">
        <div className="section-heading"><p className="eyebrow"><span /> Built for the everyday</p><h2>Everything essential,<br />nothing to babysit.</h2></div>
        <div className="feature-grid">{features.map(({ icon: Icon, title, text }) => <article className="feature-card" key={title}><Icon /><h3>{title}</h3><p>{text}</p></article>)}</div>
      </section>

      <section className="showcase">
        <div className="showcase-copy"><p className="eyebrow"><span /> Familiar, but yours</p><h2>Keep the controls<br />where they belong.</h2><p>The full remote lives in Deskora. When media is playing, the usual Android media controls stay available from the notification shade too.</p><div className="tiny-stat"><Music2 /> The phone mirrors Windows media — it does not play a second audio stream.</div></div>
        <div className="showcase-images"><figure className="shot-menu"><img src="/screenshots/deskora-menu.jpg" alt="Deskora menu with sleep, lock and connection settings" /></figure><figure className="shot-notification"><img src="/screenshots/deskora-notification.jpg" alt="Deskora media controls in the Android notification shade" /></figure></div>
      </section>

      <section className="download-section" id="downloads">
        <div><p className="eyebrow"><span /> First public test</p><h2>Try Deskora today.</h2><p>Install the agent first, then the Android test build. The Android package is not yet distributed by Google Play, so Android will ask for your approval to install it.</p></div>
        <div className="download-cards">
          <a className="download-card" href={windowsDownload} download><span className="download-icon windows"><MonitorSmartphone /></span><span><strong>Deskora Agent</strong><small>Windows 10 / 11 · v0.7.6</small></span><Download /></a>
          <a className="download-card" href={androidDownload} download><span className="download-icon android"><Smartphone /></span><span><strong>Deskora for Android</strong><small>Test APK · v0.7.4</small></span><Download /></a>
        </div>
      </section>

      <section className="privacy-strip"><LockKeyhole /><p><strong>Your connection stays yours.</strong> Deskora works between your paired phone and PC on the same local network. No account is needed.</p><a href="/privacy">Privacy details <ChevronRight /></a></section>
      <footer id="github"><a className="brand" href="#top"><span className="brand-mark" aria-hidden="true"><span /><span /></span><span>Deskora</span></a><p>Windows media, on your phone.</p><span>© 2026 Deskora · Public GitHub repository is being prepared.</span></footer>
    </main>
  );
}
