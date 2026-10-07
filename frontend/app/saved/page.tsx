"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { SiteShell } from "../../components/SiteShell";

type Property = { id: string; title: string; type: string; location: string; price: number; currency: string; bedrooms: number; bathrooms: number; area: number; areaUnit: string; imageUrl: string; featured: boolean };
const money = (value: number) => new Intl.NumberFormat("en-UG", { maximumFractionDigits: 0 }).format(value);

export default function SavedPage() {
  const [properties, setProperties] = useState<Property[]>([]);
  const [loaded, setLoaded] = useState(false);

  function getSavedIds() {
    try {
      const value: unknown = JSON.parse(localStorage.getItem("nikspeed-saved") || "[]");
      return Array.isArray(value) ? value.filter((id): id is string => typeof id === "string") : [];
    } catch { return []; }
  }

  useEffect(() => {
    let active = true;
    const load = async () => {
      const ids = getSavedIds();
      if (!ids.length) { if (active) { setProperties([]); setLoaded(true); } return; }
      try {
        const response = await fetch("/api/properties");
        if (!response.ok) throw new Error("Could not load saved homes.");
        const listings: Property[] = await response.json();
        if (active) { setProperties(ids.map(id => listings.find(item => item.id === id)).filter((item): item is Property => !!item)); setLoaded(true); }
      } catch { if (active) setLoaded(true); }
    };
    void load();
    return () => { active = false; };
  }, []);

  function remove(id: string) {
    const next = getSavedIds().filter(value => value !== id);
    localStorage.setItem("nikspeed-saved", JSON.stringify(next));
    window.dispatchEvent(new Event("nikspeed-saved-change"));
    setProperties(current => current.filter(item => item.id !== id));
  }

  return <SiteShell><main className="page"><div className="page-intro"><p className="eyebrow">YOUR SHORTLIST</p><h1>Saved homes.</h1><p>Your favourite places, kept together while you decide.</p></div>{!loaded ? <p role="status">Loading your saved homes…</p> : properties.length ? <><p className="result-count">{properties.length} saved {properties.length === 1 ? "property" : "properties"}</p><div className="grid">{properties.map(property => <article className="card" key={property.id}><div className="image" style={{ backgroundImage: `url(${property.imageUrl})` }}>{property.featured && <span>Featured</span>}</div><div className="card-body"><p className="type">{property.type}</p><h3>{property.title}</h3><p className="location">⌖ {property.location}</p><p className="price">{property.currency} {money(property.price)}</p><p className="details">{property.bedrooms ? `${property.bedrooms} beds · ${property.bathrooms} baths · ` : ""}{property.area} {property.areaUnit}</p><div className="saved-card-actions"><Link className="text-link" href={`/listings/${property.id}`}>View property →</Link><button className="saved-remove" onClick={() => remove(property.id)}>Remove</button></div></div></article>)}</div></> : <section className="empty"><h2>No saved homes yet</h2><p>Save a property while you browse, and it will be waiting here.</p><Link className="button-link" href="/listings">Explore properties</Link></section>}</main></SiteShell>;
}
