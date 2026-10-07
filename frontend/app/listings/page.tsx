"use client";
import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import { SiteShell } from "../../components/SiteShell";

type Property={id:string;title:string;type:string;location:string;price:number;currency:string;bedrooms:number;bathrooms:number;area:number;areaUnit:string;imageUrl:string;featured:boolean};
const money=(value:number)=>new Intl.NumberFormat("en-UG").format(value);

export default function ListingsPage(){
  const[properties,setProperties]=useState<Property[]>([]);
  const[type,setType]=useState("");
  const[location,setLocation]=useState("");
  const[sort,setSort]=useState("");
  const[loading,setLoading]=useState(true);
  const[error,setError]=useState("");
  async function load(event?:FormEvent){event?.preventDefault();setLoading(true);setError("");try{const query=new URLSearchParams();if(type)query.set("type",type);if(location)query.set("location",location);if(sort)query.set("sort",sort);const response=await fetch(`/api/properties?${query}`);if(!response.ok)throw new Error("Listings could not be loaded. Please try again.");setProperties(await response.json());}catch(error){setError(error instanceof Error?error.message:"The marketplace API is unavailable.");}finally{setLoading(false);}}
  useEffect(()=>{void load();},[]);
  return <SiteShell><main className="page"><div className="page-intro"><p className="eyebrow">PROPERTY SEARCH</p><h1>Explore every possibility.</h1><p>Search verified homes, apartments and land across Uganda.</p></div><form className="catalog-filters" onSubmit={load}><select value={type} onChange={event=>setType(event.target.value)}><option value="">All property types</option><option>House for sale</option><option>Apartment for rent</option><option>Land</option></select><input value={location} onChange={event=>setLocation(event.target.value)} placeholder="Location"/><select value={sort} onChange={event=>setSort(event.target.value)}><option value="">Featured first</option><option value="price-asc">Lowest price</option><option value="price-desc">Highest price</option></select><button>Search</button></form><p className="result-count" role={error?"alert":undefined}>{loading?"Searching…":error||`${properties.length} properties found`}</p>{!loading&&!error&&!properties.length&&<p>No properties match those filters. Try a different location or property type.</p>}<div className="grid">{properties.map(property=><article className="card" key={property.id}><div className="image" style={{backgroundImage:`url(${property.imageUrl})`}}>{property.featured&&<span>Featured</span>}</div><div className="card-body"><p className="type">{property.type}</p><h3>{property.title}</h3><p className="location">⌖ {property.location}</p><p className="price">{property.currency} {money(property.price)}</p><p className="details">{property.bedrooms?`${property.bedrooms} beds · ${property.bathrooms} baths · `:""}{property.area} {property.areaUnit}</p><Link className="text-link" href={`/listings/${property.id}`}>View property →</Link></div></article>)}</div></main></SiteShell>
}
