import { apiFetch, toQueryString } from "./api-client";
import { siteConfig } from "./site-config";
import type {
  FeaturedProperties,
  PagedResponse,
  Property,
  PropertyDetails,
  PropertyInput,
  PropertyLocation,
  PropertyPurpose,
  PropertySearch,
  PropertySortOrder,
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

export type PropertyListFilter = {
  search?: string;
  status?: PropertyStatus;
  type?: PropertyType;
  purpose?: PropertyPurpose;
  sort?: PropertySortOrder;
  page?: number;
};

export const listProperties = (filter: PropertyListFilter) =>
  apiFetch<PagedResponse<PropertySummary>>(`/properties/manage${toQueryString({ ...filter, pageSize: 20 })}`);

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

export function uploadPropertyImages(id: string, files: File[]) {
  const formData = new FormData();
  for (const file of files) formData.append("files", file);
  return apiFetch<Property>(`/properties/${id}/images`, { method: "POST", formData });
}

export const removePropertyImage = (id: string, fileName: string) =>
  apiFetch<Property>(`/properties/${id}/images/${encodeURIComponent(fileName)}`, { method: "DELETE" });

export const reorderPropertyImages = (id: string, fileNames: string[]) =>
  apiFetch<Property>(`/properties/${id}/images/order`, { method: "PUT", body: { fileNames } });
