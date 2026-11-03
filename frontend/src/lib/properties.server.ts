import { cacheLife } from "next/cache";
import { connection } from "next/server";
import { toQueryString } from "./api-client";
import { siteConfig } from "./site-config";
import type { FeaturedProperties, PagedResponse, PropertyDetails, PropertySearch, PropertySummary } from "./properties.types";

/**
 * Resposta pública da API guardada em memória no servidor e compartilhada por todos os visitantes.
 * Cada resposta vale 1 minuto; depois disso a próxima visita busca de novo na API, então o que o admin altera aparece
 * no site em até 1 minuto. `expire` limita a 5 minutos o tempo máximo de uma entrada.
 * Falha da API lança erro, e entrada com erro não fica no cache.
 */
async function fetchPublic<T>(path: string): Promise<T | null> {
  "use cache";
  cacheLife({ stale: 60, revalidate: 60, expire: 300 });

  const baseUrl = process.env.API_URL ?? siteConfig.apiUrl;
  const response = await fetch(`${baseUrl}${path}`, { headers: { Accept: "application/json" } });
  if (response.status === 404) return null;
  if (!response.ok) throw new Error(`A API respondeu ${response.status} em ${path}.`);
  return (await response.json()) as T;
}

/** Leitura pública feita no servidor (Server Components). Retorna null em 404 ou se a API estiver fora do ar. */
async function getPublic<T>(path: string): Promise<T | null> {
  // Só em tempo de requisição: o build não chama a API nem grava imóveis no HTML estático.
  await connection();
  try {
    return await fetchPublic<T>(path);
  } catch {
    return null;
  }
}

export const searchProperties = (search: PropertySearch) =>
  getPublic<PagedResponse<PropertySummary>>(`/properties${toQueryString(search)}`);

export const getFeaturedProperties = () => getPublic<FeaturedProperties>("/properties/featured");

export const getPropertyDetails = (id: string) => getPublic<PropertyDetails>(`/properties/${encodeURIComponent(id)}`);
