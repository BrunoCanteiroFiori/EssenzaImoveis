"use client";

import { useSyncExternalStore } from "react";
import { Monitor, Moon, Sun } from "lucide-react";
import { applyTheme, themeStorageKey, type ThemePreference } from "@/lib/theme";

const order: ThemePreference[] = ["light", "dark", "system"];
const labels: Record<ThemePreference, string> = { light: "Tema claro", dark: "Tema escuro", system: "Tema do sistema" };
const listeners = new Set<() => void>();

function readPreference(): ThemePreference {
  try {
    const stored = localStorage.getItem(themeStorageKey);
    return stored === "light" || stored === "dark" ? stored : "system";
  } catch {
    return "system";
  }
}

function subscribe(listener: () => void) {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

/** Alterna entre tema claro, escuro e o do sistema. A escolha fica salva no navegador. */
export function ThemeToggle() {
  const preference = useSyncExternalStore(subscribe, readPreference, () => "system" as ThemePreference);
  const next = order[(order.indexOf(preference) + 1) % order.length];
  const Icon = preference === "light" ? Sun : preference === "dark" ? Moon : Monitor;

  function toggle() {
    try {
      localStorage.setItem(themeStorageKey, next);
    } catch {
      // sem armazenamento: aplica só nesta página
    }
    applyTheme(next);
    listeners.forEach((listener) => listener());
  }

  return (
    <button
      type="button"
      onClick={toggle}
      className="inline-flex size-9 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-surface-muted hover:text-foreground"
      aria-label={`${labels[preference]} (clique para ${labels[next].toLowerCase()})`}
      title={labels[preference]}
    >
      <Icon className="size-5" aria-hidden />
    </button>
  );
}
