"use client";

import { FormEvent, useState } from "react";

const emailAddress = "nikspeedgobal@gmail.com";

export function ContactAssistant() {
  const [open, setOpen] = useState(false);
  const [tab, setTab] = useState<"home" | "message">("home");
  const [notice, setNotice] = useState("");

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const name = String(form.get("name") || "").trim();
    const email = String(form.get("email") || "").trim();
    const phone = String(form.get("phone") || "").trim();
    const countryCode = String(form.get("countryCode") || "+256");
    const message = String(form.get("message") || "").trim();
    const subject = encodeURIComponent(`Website enquiry from ${name}`);
    const body = encodeURIComponent(`Name: ${name}\nEmail: ${email}\nPhone: ${phone ? `${countryCode} ${phone}` : "Not provided"}\n\n${message}`);
    setNotice("Opening your email app to send this message.");
    window.location.href = `mailto:${emailAddress}?subject=${subject}&body=${body}`;
  }

  return <div className="contact-assistant-root">
    {open && <section className="contact-assistant-panel" aria-label="Website assistant">
      <div className="contact-assistant-heading">
        <div><span className="contact-assistant-status"/> <strong>Nik-Speed Assistant</strong><small>We’re here to help</small></div>
        <button type="button" className="contact-assistant-close" aria-label="Close assistant" onClick={() => setOpen(false)}>×</button>
      </div>
      {tab === "home" ? <div className="contact-assistant-welcome">
        <span className="contact-assistant-mark" aria-hidden="true">N</span>
        <p className="contact-assistant-kicker">WELCOME</p>
        <h2>How can we help?</h2>
        <p>Ask us about a property, publishing a listing, or making a payment. Send a message and our team will follow up.</p>
        <button type="button" onClick={() => { setTab("message"); setNotice(""); }}>Start a conversation <span aria-hidden="true">→</span></button>
      </div> : <form className="contact-assistant-form" onSubmit={submit}>
        <p className="contact-assistant-kicker">SEND US A MESSAGE</p>
        <label className="assistant-sr-only" htmlFor="assistant-name">Name</label>
        <input id="assistant-name" name="name" placeholder="* Name" autoComplete="name" required />
        <label className="assistant-sr-only" htmlFor="assistant-email">Email</label>
        <input id="assistant-email" name="email" type="email" placeholder="* Email" autoComplete="email" required />
        <label className="assistant-sr-only" htmlFor="assistant-phone">Phone</label>
        <div className="contact-assistant-phone"><select name="countryCode" aria-label="Country calling code" defaultValue="+256"><option value="+256">🇺🇬 +256</option><option value="+1">🇺🇸 +1</option><option value="+254">🇰🇪 +254</option><option value="+255">🇹🇿 +255</option><option value="+250">🇷🇼 +250</option><option value="+44">🇬🇧 +44</option></select><input id="assistant-phone" name="phone" type="tel" placeholder="Phone (optional)" autoComplete="tel" /></div>
        <label className="assistant-sr-only" htmlFor="assistant-message">Message</label>
        <textarea id="assistant-message" name="message" placeholder="* Message" required />
        {notice && <p className="contact-assistant-notice" role="status">{notice}</p>}
        <button className="contact-assistant-send" type="submit">Send message <span aria-hidden="true">→</span></button>
      </form>}
      <nav className="contact-assistant-tabs" aria-label="Assistant navigation">
        <button type="button" aria-label="Assistant home" aria-current={tab === "home" ? "page" : undefined} className={tab === "home" ? "active" : ""} onClick={() => { setTab("home"); setNotice(""); }}><svg viewBox="0 0 24 24" aria-hidden="true"><path d="m3.5 10 8.5-7 8.5 7v10.5h-6v-7h-5v7h-6z"/></svg></button>
        <button type="button" aria-label="Write a message" aria-current={tab === "message" ? "page" : undefined} className={tab === "message" ? "active" : ""} onClick={() => { setTab("message"); setNotice(""); }}><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M5 4.5h14a3 3 0 0 1 3 3v9a3 3 0 0 1-3 3h-9l-5 2v-5a3 3 0 0 1-3-3v-6a3 3 0 0 1 3-3z"/></svg></button>
      </nav>
    </section>}
    <button type="button" className={`contact-assistant-launcher${open ? " is-open" : ""}`} aria-label={open ? "Close website assistant" : "Open website assistant"} aria-expanded={open} onClick={() => setOpen(!open)}>
      {open ? <span aria-hidden="true">×</span> : <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 5.5h16v11H9l-5 3z"/><path d="M8 10h8M8 13h5"/></svg>}
    </button>
  </div>;
}
