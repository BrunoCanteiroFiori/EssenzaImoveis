import { apiFetch, toQueryString } from "./api-client";
import type {
  PagedResponse,
  Property,
  PropertyInput,
  PropertyLocation,
  PropertyManagementSummary,
  PropertyPurpose,
  PropertySortField,
  PropertyStatus,
  PropertySummary,
  PropertyType,
} from "./properties.types";

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
