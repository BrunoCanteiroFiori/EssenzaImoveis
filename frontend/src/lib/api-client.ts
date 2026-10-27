import { siteConfig } from "./site-config";

/** Erro de uma resposta da API (ProblemDetails). */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
    readonly fieldErrors: Record<string, string[]> = {},
  ) {
    super(message);
    this.name = "ApiError";
  }
}

type ProblemDetails = { title?: string; detail?: string; errors?: Record<string, string[]> };

type RequestOptions = {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  body?: unknown;
  formData?: FormData;
  signal?: AbortSignal;
};

const fallbackMessages: Record<number, string> = {
  401: "Sua sessão expirou. Entre novamente.",
  403: "Você não tem permissão para esta ação.",
  404: "Registro não encontrado.",
  409: "Conflito com um registro existente.",
  429: "Muitas tentativas. Aguarde alguns minutos e tente de novo.",
};

/** Rotas que não disparam renovação automática da sessão. */
const sessionPaths = ["/auth/login", "/auth/refresh", "/auth/logout"];

let refreshing: Promise<boolean> | null = null;

/** Renova a sessão uma única vez, mesmo com várias requisições recebendo 401 ao mesmo tempo. */
function refreshSession(): Promise<boolean> {
  refreshing ??= fetch(`${siteConfig.apiUrl}/auth/refresh`, { method: "POST", credentials: "include" })
    .then((response) => response.ok)
    .catch(() => false)
    .finally(() => {
      refreshing = null;
    });
  return refreshing;
}

function send(path: string, { method = "GET", body, formData, signal }: RequestOptions) {
  return fetch(`${siteConfig.apiUrl}${path}`, {
    method,
    credentials: "include",
    cache: "no-store",
    signal,
    headers: body !== undefined ? { "Content-Type": "application/json" } : undefined,
    body: formData ?? (body !== undefined ? JSON.stringify(body) : undefined),
  });
}

async function toApiError(response: Response): Promise<ApiError> {
  let problem: ProblemDetails = {};
  try {
    problem = (await response.json()) as ProblemDetails;
  } catch {
    // resposta sem corpo JSON
  }

  const fieldErrors = problem.errors ?? {};
  const firstFieldError = Object.values(fieldErrors)[0]?.[0];
  const message = problem.detail ?? firstFieldError ?? fallbackMessages[response.status] ?? "Não foi possível concluir a operação.";
  return new ApiError(response.status, message, fieldErrors);
}

/**
 * Chamada à API com os cookies de sessão (httpOnly). Em 401, tenta renovar a sessão uma vez e repete a chamada.
 */
export async function apiFetch<T>(path: string, options: RequestOptions = {}): Promise<T> {
  let response = await send(path, options);
  if (response.status === 401 && !sessionPaths.includes(path) && (await refreshSession())) {
    response = await send(path, options);
  }

  if (!response.ok) {
    throw await toApiError(response);
  }

  if (response.status === 204 || response.status === 202) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

/** Monta a query string ignorando valores vazios. */
export function toQueryString(params: Record<string, string | number | boolean | null | undefined>) {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== null && value !== undefined && value !== "") search.set(key, String(value));
  }
  const query = search.toString();
  return query ? `?${query}` : "";
}
