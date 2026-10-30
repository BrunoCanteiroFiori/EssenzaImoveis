export type PropertyType = "House" | "Apartment" | "Land" | "Commercial" | "Rural";
export type PropertyPurpose = "Sale" | "Rent";
export type PropertyStatus = "Available" | "Reserved" | "Sold" | "Rented" | "Inactive";
export type PropertySortOrder = "Relevance" | "Newest" | "PriceAscending" | "PriceDescending" | "Hottest";
export type PropertySortField = "Title" | "Type" | "Price" | "Status" | "Temperature";
export type TemperatureLevel = "Cold" | "Warm" | "Hot" | "VeryHot";

export type Temperature = { recentViews: number; level: TemperatureLevel };

export type PagedResponse<TItem> = {
  items: TItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
};

export type PropertySummary = {
  id: string;
  title: string;
  type: PropertyType;
  purpose: PropertyPurpose;
  status: PropertyStatus;
  price: number;
  area: number;
  bedrooms: number;
  bathrooms: number;
  parkingSpaces: number;
  neighborhood: string;
  city: string;
  state: string;
  coverImageUrl: string | null;
  mediaCount: number;
  createdAt: string;
};

/** Resumo da área administrativa. Temperatura e patrocínio não saem na área pública. */
export type PropertyManagementSummary = PropertySummary & {
  isSponsored: boolean;
  temperature: Temperature;
};

export type PropertyDetails = {
  id: string;
  title: string;
  description: string;
  type: PropertyType;
  purpose: PropertyPurpose;
  price: number;
  condominiumFee: number | null;
  propertyTax: number | null;
  area: number;
  bedrooms: number;
  bathrooms: number;
  parkingSpaces: number;
  neighborhood: string;
  city: string;
  state: string;
  coverImageUrl: string | null;
  media: PropertyMedia[];
  createdAt: string;
};

export type Address = {
  street: string;
  number: string | null;
  complement: string | null;
  neighborhood: string;
  city: string;
  state: string;
  zipCode: string;
};

export type PropertyMediaType = "Photo" | "Video";

/** Foto ou vídeo do carrossel. A capa do anúncio é a primeira foto. */
export type PropertyMedia = { fileName: string; type: PropertyMediaType; url: string };

/** Imóvel completo da área administrativa. */
export type Property = {
  id: string;
  title: string;
  description: string;
  type: PropertyType;
  purpose: PropertyPurpose;
  status: PropertyStatus;
  price: number;
  condominiumFee: number | null;
  propertyTax: number | null;
  area: number;
  bedrooms: number;
  bathrooms: number;
  parkingSpaces: number;
  address: Address;
  media: PropertyMedia[];
  sponsoredUntil: string | null;
  isSponsored: boolean;
  createdAt: string;
  updatedAt: string;
};

export type PropertyInput = Omit<
  Property,
  "id" | "status" | "media" | "sponsoredUntil" | "isSponsored" | "createdAt" | "updatedAt"
>;

export type FeaturedProperties = {
  highlights: PropertySummary[];
  forSale: PropertySummary[];
  forRent: PropertySummary[];
  mostSearched: PropertySummary[];
  newest: PropertySummary[];
};

export type PropertyLocation = { neighborhood: string; city: string; state: string };

export type PropertySearch = {
  location?: string;
  type?: PropertyType;
  purpose?: PropertyPurpose;
  minBedrooms?: number;
  minBathrooms?: number;
  minParkingSpaces?: number;
  minArea?: number;
  maxArea?: number;
  minPrice?: number;
  maxPrice?: number;
  sort?: PropertySortOrder;
  page?: number;
};
