import { useSyncExternalStore } from "react";
import { getSavedProperties, removeSavedProperty, saveProperty } from "./properties.api";
import type { PropertySummary } from "./properties.types";
import { getVisitorId } from "./visitor";

type SavedProperties = { items: PropertySummary[]; ids: ReadonlySet<string> };

const noSavedProperties: SavedProperties = { items: [], ids: new Set() };
const listeners = new Set<() => void>();

let state = noSavedProperties;
let loadRequested = false;
let loading: Promise<void> | null = null;

function setState(next: SavedProperties) {
  state = next;
  listeners.forEach((listener) => listener());
}

function setItems(items: PropertySummary[]) {
  setState({ items, ids: new Set(items.map((item) => item.id)) });
}

/** Lê os salvos da API. Se falhar, mantém o que já está na tela. */
export function loadSavedProperties() {
  loading ??= getSavedProperties(getVisitorId())
    .then(setItems)
    .catch(() => undefined)
    .finally(() => {
      loading = null;
    });
  return loading;
}

function subscribe(listener: () => void) {
  listeners.add(listener);
  if (!loadRequested) {
    loadRequested = true;
    void loadSavedProperties();
  }
  return () => listeners.delete(listener);
}

/** Salva ou tira dos salvos. O coração muda na hora e volta ao que era se a API recusar. */
export async function toggleSavedProperty(id: string) {
  const previous = state;
  const wasSaved = previous.ids.has(id);
  const ids = new Set(previous.ids);
  if (wasSaved) ids.delete(id);
  else ids.add(id);
  setState({ items: wasSaved ? previous.items.filter((item) => item.id !== id) : previous.items, ids });

  const visitorId = getVisitorId();
  try {
    if (wasSaved) {
      await removeSavedProperty(id, visitorId);
    } else {
      await saveProperty(id, visitorId);
      setItems(await getSavedProperties(visitorId));
    }
  } catch {
    setState(previous);
  }
}

/** Salvos do visitante, compartilhados por todos os corações e pela seção da página inicial. */
export function useSavedProperties() {
  return useSyncExternalStore(subscribe, () => state, () => noSavedProperties);
}
