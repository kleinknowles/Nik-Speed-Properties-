import "./globals.css";
import "./marketplace.css";
import "./pages.css";

export const metadata = { title: "Nik Speed Properties LLC", description: "Find homes, rentals and land across Uganda with Nik Speed Properties LLC." };

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="en"><body><div className="ambient-backdrop" aria-hidden="true"><div className="ambient-photo ambient-photo-one"/><div className="ambient-photo ambient-photo-two"/><div className="ambient-photo ambient-photo-three"/><div className="ambient-glow"/></div><div className="site-content">{children}</div></body></html>;
}
