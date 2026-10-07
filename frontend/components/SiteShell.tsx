"use client";

import Link from "next/link";
import { ReactNode } from "react";
import { useEffect, useState } from "react";

export function Header() {
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

  return <header><Link className="brand" href="/" aria-label="Nik Speed Properties LLC home">Nik Speed <span>Properties LLC</span></Link><nav><Link href="/listings">Browse homes</Link><Link href="/saved">Saved homes{savedCount > 0 ? ` (${savedCount})` : ""}</Link><Link href="/advertise">Advertise</Link><Link href="/about">About us</Link><Link className="nav-cta" href="/advertise">List a property</Link></nav></header>;
}

export function Footer() {
  return <footer><div><Link className="brand" href="/">Nik Speed <span>Properties LLC</span></Link><p>Property decisions, made simpler.</p></div><div className="footer-links"><Link href="/contact">Contact</Link><Link href="/about">About</Link><Link href="/terms">Terms</Link><Link href="/privacy">Privacy</Link></div><p>© 2026 Nik Speed Properties LLC</p></footer>;
}

export function SiteShell({ children }: { children: ReactNode }) { return <><Header />{children}<Footer /></>; }
