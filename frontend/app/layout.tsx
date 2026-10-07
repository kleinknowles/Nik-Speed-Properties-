import "./globals.css";
import "./marketplace.css";
import "./pages.css";

export const metadata = { title: "Nik Speed Properties LLC", description: "Find homes, rentals and land across Uganda with Nik Speed Properties LLC." };

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="en"><body>{children}</body></html>;
}
