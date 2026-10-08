"use client";

import { useEffect, useState } from "react";

type InstallPromptEvent = Event & { prompt: () => Promise<void>; userChoice: Promise<{ outcome: "accepted" | "dismissed" }> };

export function InstallPrompt() {
  const [installEvent, setInstallEvent] = useState<InstallPromptEvent | null>(null);
  const [installed, setInstalled] = useState(false);
  const [showHelp, setShowHelp] = useState(false);
  const [dismissed, setDismissed] = useState(false);

  useEffect(() => {
    setInstalled(window.matchMedia("(display-mode: standalone)").matches || (navigator as Navigator & { standalone?: boolean }).standalone === true);
    const onPrompt = (event: Event) => {
      event.preventDefault();
      setInstallEvent(event as InstallPromptEvent);
    };
    const onInstalled = () => { setInstalled(true); setInstallEvent(null); setShowHelp(false); };
    window.addEventListener("beforeinstallprompt", onPrompt);
    window.addEventListener("appinstalled", onInstalled);
    if ("serviceWorker" in navigator) navigator.serviceWorker.register("/sw.js").catch(() => undefined);
    return () => {
      window.removeEventListener("beforeinstallprompt", onPrompt);
      window.removeEventListener("appinstalled", onInstalled);
    };
  }, []);

  if (installed || dismissed) return null;

  const install = async () => {
    if (!installEvent) { setShowHelp(true); return; }
    await installEvent.prompt();
    const choice = await installEvent.userChoice;
    if (choice.outcome === "accepted") setInstalled(true);
    setInstallEvent(null);
  };

  return <aside className="install-prompt" aria-label="Install Nik-Speed Properties">
    <div className="install-app-icon" aria-hidden="true">N</div>
    <div className="install-prompt-copy"><strong>Take Nik-Speed Properties with you</strong><span>{showHelp ? "In your browser menu, choose ‘Install app’ or ‘Add to Home Screen’." : "Install the lightweight app for quick access from your desktop or home screen."}</span></div>
    <button type="button" onClick={install}>{installEvent ? "Install app" : showHelp ? "Got it" : "How to install"}</button>
    <button type="button" className="install-dismiss" aria-label="Dismiss install prompt" onClick={() => setDismissed(true)}>×</button>
  </aside>;
}
