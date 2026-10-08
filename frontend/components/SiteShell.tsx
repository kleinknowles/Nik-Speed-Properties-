"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { ReactNode } from "react";
import { useEffect, useState } from "react";

export function Header({ onSignIn, onRegister }: { onSignIn?: () => void; onRegister?: () => void }) {
  const pathname = usePathname();
  const [savedCount, setSavedCount] = useState(0);
  const pageThemes: Record<string, string> = {
    "/": "home",
    "/listings": "listings",
    "/saved": "saved",
    "/advertise": "advertise",
    "/about": "about",
    "/contact": "contact",
    "/dashboard": "dashboard",
    "/payment/complete": "payment",
    "/terms": "terms",
    "/privacy": "privacy",
  };
  const theme = pathname.startsWith("/listings/") ? "property" : pageThemes[pathname] || "home";

  useEffect(() => {
    const updateSavedCount = () => {
      try {
        const saved = JSON.parse(localStorage.getItem("nikspeed-saved") || "[]");
        setSavedCount(Array.isArray(saved) ? saved.length : 0);
      } catch { setSavedCount(0); }
    };
    updateSavedCount();
    window.addEventListener("storage", updateSavedCount);
    window.addEventListener("nikspeed-saved-change", updateSavedCount);
    return () => {
      window.removeEventListener("storage", updateSavedCount);
      window.removeEventListener("nikspeed-saved-change", updateSavedCount);
    };
  }, []);

  return <header className="site-header" data-page-theme={theme}><Link className="brand" href="/" aria-label="Nik Speed Properties LLC home">Nik Speed <span>Properties LLC</span></Link><nav className="site-nav" aria-label="Main navigation"><Link href="/listings" aria-current={theme === "listings" || theme === "property" ? "page" : undefined}>Browse homes</Link><Link href="/saved" aria-current={theme === "saved" ? "page" : undefined}>Saved homes{savedCount > 0 ? ` (${savedCount})` : ""}</Link><Link href="/advertise" aria-current={theme === "advertise" ? "page" : undefined}>Advertise</Link><Link href="/about" aria-current={theme === "about" ? "page" : undefined}>About us</Link><Link href="/contact" aria-current={theme === "contact" ? "page" : undefined}>Contact</Link><Link href="/dashboard" aria-current={theme === "dashboard" ? "page" : undefined}>Dashboard</Link>{theme === "home" && onSignIn && <button type="button" className="home-signin" onClick={onSignIn}>Sign in</button>}{theme === "home" && onRegister && <button type="button" className="home-register" onClick={onRegister}>Create account</button>}</nav></header>;
}

export function Footer() {
  return <footer><div><Link className="brand" href="/">Nik Speed <span>Properties LLC</span></Link><p>Property decisions, made simpler.</p></div><div className="footer-links"><Link href="/contact">Contact</Link><Link href="/about">About</Link><Link href="/terms">Terms</Link><Link href="/privacy">Privacy</Link></div><p>© 2026 Nik Speed Properties LLC</p></footer>;
}

export function SiteShell({ children, onSignIn, onRegister }: { children: ReactNode; onSignIn?: () => void; onRegister?: () => void }) { return <><Header onSignIn={onSignIn} onRegister={onRegister} />{children}<Footer /></>; }
