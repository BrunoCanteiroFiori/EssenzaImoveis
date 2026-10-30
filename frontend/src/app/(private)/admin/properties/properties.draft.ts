import { formatDecimalInput, formatZipCode, parseDecimalInput } from "@/lib/format";
import type { Property, PropertyInput, PropertyPurpose, PropertyType } from "@/lib/properties.types";

/** Valores do formulário como foram digitados (com máscara). O envio à API e a prévia partem daqui. */
export type PropertyDraft = {
  title: string;
  description: string;
  type: PropertyType;
  purpose: PropertyPurpose;
  price: string;
  condominiumFee: string;
  propertyTax: string;
  area: string;
  bedrooms: string;
  bathrooms: string;
  parkingSpaces: string;
  zipCode: string;
  street: string;
  number: string;
  complement: string;
  neighborhood: string;
  city: string;
  state: string;
};

const decimalText = (value: number | null | undefined) => (value ? formatDecimalInput(value.toFixed(2)) : "");

const optionalText = (value: string) => value.trim() || null;

export function toPropertyDraft(property?: Property): PropertyDraft {
  return {
    title: property?.title ?? "",
    description: property?.description ?? "",
    type: property?.type ?? "House",
    purpose: property?.purpose ?? "Sale",
    price: decimalText(property?.price),
    condominiumFee: decimalText(property?.condominiumFee),
    propertyTax: decimalText(property?.propertyTax),
    area: decimalText(property?.area),
    bedrooms: String(property?.bedrooms ?? 0),
    bathrooms: String(property?.bathrooms ?? 0),
    parkingSpaces: String(property?.parkingSpaces ?? 0),
    zipCode: formatZipCode(property?.address.zipCode ?? ""),
    street: property?.address.street ?? "",
    number: property?.address.number ?? "",
    complement: property?.address.complement ?? "",
    neighborhood: property?.address.neighborhood ?? "",
    city: property?.address.city ?? "",
    state: property?.address.state ?? "SP",
  };
}

export function toPropertyInput(draft: PropertyDraft): PropertyInput {
  const isLand = draft.type === "Land";
  return {
    title: draft.title,
    description: draft.description,
    type: draft.type,
    purpose: draft.purpose,
    price: parseDecimalInput(draft.price) ?? 0,
    condominiumFee: parseDecimalInput(draft.condominiumFee),
    propertyTax: parseDecimalInput(draft.propertyTax),
    area: parseDecimalInput(draft.area) ?? 0,
    bedrooms: isLand ? 0 : Number(draft.bedrooms),
    bathrooms: isLand ? 0 : Number(draft.bathrooms),
    parkingSpaces: Number(draft.parkingSpaces),
    address: {
      street: draft.street,
      number: optionalText(draft.number),
      complement: optionalText(draft.complement),
      neighborhood: draft.neighborhood,
      city: draft.city,
      state: draft.state,
      zipCode: draft.zipCode,
    },
  };
}
