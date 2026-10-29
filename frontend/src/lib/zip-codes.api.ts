import { apiFetch } from "./api-client";

/** Endereço de um CEP, com o texto já normalizado pela API. Logradouro e bairro vêm nulos em CEP geral de cidade. */
export type ZipCodeAddress = {
  zipCode: string;
  street: string | null;
  neighborhood: string | null;
  city: string;
  state: string;
};

export const getZipCodeAddress = (zipCode: string, signal?: AbortSignal) =>
  apiFetch<ZipCodeAddress>(`/zip-codes/${encodeURIComponent(zipCode)}`, { signal });
