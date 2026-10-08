"use client";

import Link from "next/link";
import { ReactNode } from "react";
import { useEffect, useState } from "react";

export function Header({ onSignIn, onRegister }: { onSignIn?: () => void; onRegister?: () => void }) {
  const [savedCount, setSavedCount] = useState(0);

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

  return <header className="site-header"><Link className="brand" href="/" aria-label="Nik-Speed Properties LLC home">Nik Speed <span>Properties LLC</span></Link><nav className="site-nav" aria-label="Main navigation"><Link href="/listings">Browse homes</Link><Link href="/saved">Saved homes{savedCount > 0 ? ` (${savedCount})` : ""}</Link><Link href="/advertise">Advertise</Link><Link href="/about">About us</Link><Link href="/contact">Contact</Link><Link href="/dashboard">Dashboard</Link>{onSignIn && <button type="button" className="home-signin" onClick={onSignIn}>Sign in</button>}{onRegister && <button type="button" className="home-register" onClick={onRegister}>Create account</button>}</nav></header>;
}

export function Footer() {
  return <footer><div><Link className="brand" href="/">Nik Speed <span>Properties LLC</span></Link><p>Property decisions, made simpler.</p></div><div className="footer-navigation"><div className="footer-links"><Link href="/contact">Contact</Link><Link href="/about">About</Link><Link href="/terms">Terms</Link><Link href="/privacy">Privacy</Link></div><small className="footer-credit">Designed &amp; Assembled by Nik-Speed I.T Solution &amp; Internet Services</small></div><p>© 2026 Nik-Speed Properties LLC</p></footer>;
}

export function SiteShell({ children, onSignIn, onRegister }: { children: ReactNode; onSignIn?: () => void; onRegister?: () => void }) { return <><Header onSignIn={onSignIn} onRegister={onRegister} />{children}<Footer /></>; }
