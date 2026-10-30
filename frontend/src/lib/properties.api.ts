import { apiFetch, toQueryString } from "./api-client";
import { siteConfig } from "./site-config";
import type {
  FeaturedProperties,
  PagedResponse,
  Property,
  PropertyDetails,
  PropertyInput,
  PropertyLocation,
  PropertyManagementSummary,
  PropertyPurpose,
  PropertySearch,
  PropertySortField,
  PropertyStatus,
  PropertySummary,
  PropertyType,
} from "./properties.types";

/** Leitura pública feita no servidor (Server Components). Retorna null em 404 ou se a API estiver fora do ar. */
async function getPublic<T>(path: string): Promise<T | null> {
  const baseUrl = process.env.API_URL ?? siteConfig.apiUrl;
  try {
    const response = await fetch(`${baseUrl}${path}`, { headers: { Accept: "application/json" } });
    return response.ok ? ((await response.json()) as T) : null;
  } catch {
    return null;
  }
}

export const searchProperties = (search: PropertySearch) =>
  getPublic<PagedResponse<PropertySummary>>(`/properties${toQueryString(search)}`);

export const getFeaturedProperties = () => getPublic<FeaturedProperties>("/properties/featured");

export const getPropertyDetails = (id: string) => getPublic<PropertyDetails>(`/properties/${encodeURIComponent(id)}`);

export const getPropertyLocations = () => apiFetch<PropertyLocation[]>("/properties/locations");

export const registerPropertyView = (id: string, visitorId: string) =>
  apiFetch<void>(`/properties/${id}/views`, { method: "POST", body: { visitorId } });

export const getRecentlyViewedProperties = (visitorId: string) =>
  apiFetch<PropertySummary[]>("/properties/recently-viewed", { visitorId });

export const getPropertySuggestions = (id: string, visitorId: string, signal: AbortSignal) =>
  apiFetch<PropertySummary[]>(`/properties/${id}/suggestions`, { visitorId, signal });

export const getSavedProperties = (visitorId: string) => apiFetch<PropertySummary[]>("/saved-properties", { visitorId });

export const saveProperty = (id: string, visitorId: string) =>
  apiFetch<void>(`/saved-properties/${id}`, { method: "PUT", visitorId });

export const removeSavedProperty = (id: string, visitorId: string) =>
  apiFetch<void>(`/saved-properties/${id}`, { method: "DELETE", visitorId });

export type PropertyListFilter = {
  search?: string;
  status?: PropertyStatus;
  type?: PropertyType;
  purpose?: PropertyPurpose;
  sortField?: PropertySortField;
  sortDescending?: boolean;
  page?: number;
};

export const listProperties = (filter: PropertyListFilter) =>
  apiFetch<PagedResponse<PropertyManagementSummary>>(`/properties/manage${toQueryString({ ...filter, pageSize: 20 })}`);

export const getProperty = (id: string) => apiFetch<Property>(`/properties/manage/${id}`);

export const createProperty = (input: PropertyInput) => apiFetch<Property>("/properties", { method: "POST", body: input });

export const updateProperty = (id: string, input: PropertyInput) =>
  apiFetch<Property>(`/properties/${id}`, { method: "PUT", body: input });

export const deleteProperty = (id: string) => apiFetch<void>(`/properties/${id}`, { method: "DELETE" });

export const changePropertyStatus = (id: string, status: PropertyStatus) =>
  apiFetch<Property>(`/properties/${id}/status`, { method: "PATCH", body: { status } });

export const sponsorProperty = (id: string, days: number) =>
  apiFetch<Property>(`/properties/${id}/sponsorship`, { method: "PUT", body: { days } });

export const removePropertySponsorship = (id: string) =>
  apiFetch<Property>(`/properties/${id}/sponsorship`, { method: "DELETE" });

export function uploadPropertyMedia(id: string, files: File[]) {
  const formData = new FormData();
  for (const file of files) formData.append("files", file);
  return apiFetch<Property>(`/properties/${id}/media`, { method: "POST", formData });
}

export const removePropertyMedia = (id: string, fileName: string) =>
  apiFetch<Property>(`/properties/${id}/media/${encodeURIComponent(fileName)}`, { method: "DELETE" });

export const reorderPropertyMedia = (id: string, fileNames: string[]) =>
  apiFetch<Property>(`/properties/${id}/media/order`, { method: "PUT", body: { fileNames } });
