type Props = { location: string; title?: string };

export function LocationLink({ location }: Props) {
  const query = `${location}, Uganda`;
  return <a className="location-map-link" href={`https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(query)}`} target="_blank" rel="noreferrer">View on map ↗</a>;
}

export function LocationMap({ location, title = "Property location" }: Props) {
  const query = encodeURIComponent(`${location}, Uganda`);
  return <section className="location-map-section"><div className="location-map-heading"><div><p className="eyebrow">LOCATION</p><h2>{title}</h2><p>Approximate area: {location}</p></div><LocationLink location={location}/></div><iframe className="location-map" title={`Map showing ${location}`} src={`https://maps.google.com/maps?q=${query}&output=embed`} loading="lazy" referrerPolicy="no-referrer-when-downgrade" allowFullScreen/></section>;
}
