import "./globals.css";
import "./marketplace.css";
import "./pages.css";
import { InstallPrompt } from "../components/InstallPrompt";
import { ContactAssistant } from "../components/ContactAssistant";

export const metadata = {
  title: "Nik-Speed Properties LLC",
  description: "Find homes, rentals and land across Uganda with Nik-Speed Properties LLC.",
  applicationName: "Nik-Speed Properties",
  manifest: "/manifest.webmanifest",
  appleWebApp: { capable: true, statusBarStyle: "default" as const, title: "Nik-Speed Properties" },
  icons: { icon: "/app-icon.svg", apple: "/app-icon.svg" },
  themeColor: "#1d2b24",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="en"><body><div className="ambient-backdrop" aria-hidden="true"><div className="ambient-photo ambient-photo-one"/><div className="ambient-photo ambient-photo-two"/><div className="ambient-photo ambient-photo-three"/><div className="ambient-glow"/></div><div className="site-content">{children}<InstallPrompt /><ContactAssistant /></div></body></html>;
}
