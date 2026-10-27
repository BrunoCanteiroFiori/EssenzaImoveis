const visitorStorageKey = "visitor-id";

/** Identificador anônimo do navegador, usado para contar visitantes únicos (a API guarda só o hash). */
export function getVisitorId(): string {
  try {
    const existing = localStorage.getItem(visitorStorageKey);
    if (existing) return existing;
    const created = crypto.randomUUID();
    localStorage.setItem(visitorStorageKey, created);
    return created;
  } catch {
    return crypto.randomUUID();
  }
}
