import type { PropertyPurpose, PropertySortOrder, PropertyStatus, PropertyType, TemperatureLevel } from "./properties.types";

export const propertyTypeLabels: Record<PropertyType, string> = {
  House: "Casa",
  Apartment: "Apartamento",
  Land: "Terreno",
  Commercial: "Comercial",
  Rural: "Rural",
};

export const propertyPurposeLabels: Record<PropertyPurpose, string> = {
  Sale: "Venda",
  Rent: "Aluguel",
};

export const propertyStatusLabels: Record<PropertyStatus, string> = {
  Available: "Disponível",
  Reserved: "Reservado",
  Sold: "Vendido",
  Rented: "Alugado",
  Inactive: "Inativo",
};

export const sortOrderLabels: Record<PropertySortOrder, string> = {
  Relevance: "Mais relevantes",
  Newest: "Mais recentes",
  PriceAscending: "Menor preço",
  PriceDescending: "Maior preço",
  Hottest: "Mais procurados",
};

export const temperatureLabels: Record<TemperatureLevel, string> = {
  Cold: "Frio",
  Warm: "Morno",
  Hot: "Quente",
  VeryHot: "Em alta",
};

export const propertyTypes = Object.keys(propertyTypeLabels) as PropertyType[];
export const propertyPurposes = Object.keys(propertyPurposeLabels) as PropertyPurpose[];
export const propertyStatuses = Object.keys(propertyStatusLabels) as PropertyStatus[];
/** Ordenações da busca pública: a ordenação por temperatura fica só na gestão. */
export const sortOrders: PropertySortOrder[] = ["Relevance", "Newest", "PriceAscending", "PriceDescending"];
