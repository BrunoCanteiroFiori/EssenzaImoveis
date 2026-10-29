"use client";

import { useId, useRef, useState, type ChangeEvent, type FormEvent } from "react";
import { LoaderCircle, Save } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Field, Input, Select, Textarea } from "@/components/ui/field";
import { Alert } from "@/components/ui/feedback";
import { ApiError } from "@/lib/api-client";
import { formatZipCode } from "@/lib/format";
import { propertyPurposeLabels, propertyPurposes, propertyTypeLabels, propertyTypes } from "@/lib/labels";
import type { Property, PropertyInput, PropertyPurpose, PropertyType } from "@/lib/properties.types";
import { getZipCodeAddress } from "@/lib/zip-codes.api";

const states = [
  "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA",
  "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO",
];

type PropertyFormProps = {
  property?: Property;
  readOnly?: boolean;
  submitLabel: string;
  onSubmit: (input: PropertyInput) => Promise<void>;
};

function optionalNumber(value: FormDataEntryValue | null) {
  const text = String(value ?? "").trim();
  return text === "" ? null : Number(text);
}

function optionalText(value: FormDataEntryValue | null) {
  const text = String(value ?? "").trim();
  return text === "" ? null : text;
}

type AddressFields = { zipCode: string; street: string; neighborhood: string; city: string; state: string };

type ZipCodeLookup = { status: "idle" | "loading" | "found" | "failed"; message?: string };

const zipCodeDigits = (zipCode: string) => zipCode.replace(/\D/g, "");

function zipCodeLookupError(exception: unknown) {
  if (exception instanceof ApiError && exception.status === 404) {
    return "CEP não encontrado. Confira o número ou preencha o endereço manualmente.";
  }
  if (exception instanceof ApiError && exception.status === 400) return exception.message;
  return "Não foi possível consultar o CEP agora. Preencha o endereço manualmente.";
}

/** Formulário de cadastro e edição dos dados do imóvel (fotos, status e destaque ficam em seções próprias). */
export function PropertyForm({ property, readOnly = false, submitLabel, onSubmit }: PropertyFormProps) {
  const idPrefix = useId();
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [saving, setSaving] = useState(false);
  const [type, setType] = useState<PropertyType>(property?.type ?? "House");
  const [address, setAddress] = useState<AddressFields>(() => ({
    zipCode: formatZipCode(property?.address.zipCode ?? ""),
    street: property?.address.street ?? "",
    neighborhood: property?.address.neighborhood ?? "",
    city: property?.address.city ?? "",
    state: property?.address.state ?? "SP",
  }));
  const [zipCodeLookup, setZipCodeLookup] = useState<ZipCodeLookup>({ status: "idle" });
  const zipCodeRequest = useRef<AbortController | null>(null);
  const autofilledAddress = useRef<Omit<AddressFields, "zipCode"> | null>(null);
  const streetInput = useRef<HTMLInputElement>(null);
  const numberInput = useRef<HTMLInputElement>(null);
  const isLand = type === "Land";

  function updateAddress(field: Exclude<keyof AddressFields, "zipCode">) {
    return (event: ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
      setAddress((current) => ({ ...current, [field]: event.target.value }));
  }

  /**
   * Preenche logradouro, bairro, cidade e UF pelo CEP. Uma consulta nova cancela a anterior.
   * Se o CEP novo falhar, apaga o que veio do CEP anterior e não foi editado, para não salvar endereço de outro CEP.
   */
  async function lookUpZipCode(zipCode: string) {
    zipCodeRequest.current?.abort();
    const request = new AbortController();
    zipCodeRequest.current = request;
    setZipCodeLookup({ status: "loading" });
    try {
      const found = await getZipCodeAddress(zipCode, request.signal);
      if (request.signal.aborted) return;
      const filled = { street: found.street ?? "", neighborhood: found.neighborhood ?? "", city: found.city, state: found.state };
      autofilledAddress.current = filled;
      setAddress((current) => ({ ...current, ...filled }));
      setZipCodeLookup({
        status: "found",
        message: found.street
          ? "Endereço preenchido pelo CEP. Confira e informe o número."
          : "CEP geral da cidade: informe o logradouro e o bairro.",
      });
      (found.street ? numberInput : streetInput).current?.focus();
    } catch (exception) {
      if (request.signal.aborted) return;
      const stale = autofilledAddress.current;
      autofilledAddress.current = null;
      if (stale) {
        setAddress((current) => ({
          ...current,
          street: current.street === stale.street ? "" : current.street,
          neighborhood: current.neighborhood === stale.neighborhood ? "" : current.neighborhood,
          city: current.city === stale.city ? "" : current.city,
        }));
      }
      setZipCodeLookup({ status: "failed", message: zipCodeLookupError(exception) });
    }
  }

  function handleZipCodeChange(event: ChangeEvent<HTMLInputElement>) {
    const zipCode = formatZipCode(event.target.value);
    const digits = zipCodeDigits(zipCode);
    const changed = digits !== zipCodeDigits(address.zipCode);
    setAddress((current) => ({ ...current, zipCode }));
    if (!changed) return;

    if (digits.length === 8) {
      void lookUpZipCode(digits);
    } else {
      zipCodeRequest.current?.abort();
      setZipCodeLookup({ status: "idle" });
    }
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const input: PropertyInput = {
      title: String(data.get("title") ?? ""),
      description: String(data.get("description") ?? ""),
      type,
      purpose: String(data.get("purpose")) as PropertyPurpose,
      price: Number(data.get("price")),
      condominiumFee: optionalNumber(data.get("condominiumFee")),
      propertyTax: optionalNumber(data.get("propertyTax")),
      area: Number(data.get("area")),
      bedrooms: isLand ? 0 : Number(data.get("bedrooms") ?? 0),
      bathrooms: isLand ? 0 : Number(data.get("bathrooms") ?? 0),
      parkingSpaces: Number(data.get("parkingSpaces") ?? 0),
      address: {
        street: String(data.get("street") ?? ""),
        number: optionalText(data.get("number")),
        complement: optionalText(data.get("complement")),
        neighborhood: String(data.get("neighborhood") ?? ""),
        city: String(data.get("city") ?? ""),
        state: String(data.get("state") ?? ""),
        zipCode: String(data.get("zipCode") ?? ""),
      },
    };

    setSaving(true);
    setError(null);
    setSuccess(false);
    try {
      await onSubmit(input);
      setSuccess(true);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Não foi possível salvar.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <form onSubmit={handleSubmit}>
      <fieldset disabled={readOnly || saving} className="flex flex-col gap-6">
        <div className="grid gap-4 md:grid-cols-2">
          <Field label="Título do anúncio" htmlFor={`${idPrefix}-title`} className="md:col-span-2">
            <Input id={`${idPrefix}-title`} name="title" required maxLength={150} defaultValue={property?.title} />
          </Field>
          <Field label="Tipo" htmlFor={`${idPrefix}-type`}>
            <Select id={`${idPrefix}-type`} name="type" value={type} onChange={(event) => setType(event.target.value as PropertyType)}>
              {propertyTypes.map((option) => (
                <option key={option} value={option}>
                  {propertyTypeLabels[option]}
                </option>
              ))}
            </Select>
          </Field>
          <Field label="Finalidade" htmlFor={`${idPrefix}-purpose`}>
            <Select id={`${idPrefix}-purpose`} name="purpose" defaultValue={property?.purpose ?? "Sale"}>
              {propertyPurposes.map((option) => (
                <option key={option} value={option}>
                  {propertyPurposeLabels[option]}
                </option>
              ))}
            </Select>
          </Field>
          <Field label="Descrição" htmlFor={`${idPrefix}-description`} className="md:col-span-2">
            <Textarea id={`${idPrefix}-description`} name="description" required maxLength={5000} rows={6} defaultValue={property?.description} />
          </Field>
        </div>

        <div className="grid gap-4 sm:grid-cols-3">
          <Field label="Valor (R$)" htmlFor={`${idPrefix}-price`} hint="Venda: valor total. Aluguel: valor mensal.">
            <Input id={`${idPrefix}-price`} name="price" type="number" required min={0.01} step={0.01} defaultValue={property?.price} />
          </Field>
          <Field label="Condomínio (R$/mês)" htmlFor={`${idPrefix}-condominiumFee`}>
            <Input id={`${idPrefix}-condominiumFee`} name="condominiumFee" type="number" min={0} step={0.01} defaultValue={property?.condominiumFee ?? ""} />
          </Field>
          <Field label="IPTU (R$/ano)" htmlFor={`${idPrefix}-propertyTax`}>
            <Input id={`${idPrefix}-propertyTax`} name="propertyTax" type="number" min={0} step={0.01} defaultValue={property?.propertyTax ?? ""} />
          </Field>
        </div>

        <div className="grid gap-4 sm:grid-cols-4">
          <Field label="Área (m²)" htmlFor={`${idPrefix}-area`}>
            <Input id={`${idPrefix}-area`} name="area" type="number" required min={0.01} step={0.01} defaultValue={property?.area} />
          </Field>
          <Field label="Quartos" htmlFor={`${idPrefix}-bedrooms`} hint={isLand ? "Não se aplica a terrenos." : undefined}>
            <Input id={`${idPrefix}-bedrooms`} name="bedrooms" type="number" min={0} max={99} disabled={isLand} defaultValue={isLand ? 0 : (property?.bedrooms ?? 0)} />
          </Field>
          <Field label="Banheiros" htmlFor={`${idPrefix}-bathrooms`}>
            <Input id={`${idPrefix}-bathrooms`} name="bathrooms" type="number" min={0} max={99} disabled={isLand} defaultValue={isLand ? 0 : (property?.bathrooms ?? 0)} />
          </Field>
          <Field label="Vagas de garagem" htmlFor={`${idPrefix}-parkingSpaces`}>
            <Input id={`${idPrefix}-parkingSpaces`} name="parkingSpaces" type="number" min={0} max={99} defaultValue={property?.parkingSpaces ?? 0} />
          </Field>
        </div>

        <div className="grid gap-4 sm:grid-cols-6">
          <Field
            label="CEP"
            htmlFor={`${idPrefix}-zipCode`}
            className="sm:col-span-2"
            hint={
              zipCodeLookup.status === "loading"
                ? "Buscando endereço..."
                : zipCodeLookup.status === "found"
                  ? zipCodeLookup.message
                  : "Preenche o endereço automaticamente."
            }
            error={zipCodeLookup.status === "failed" ? zipCodeLookup.message : undefined}
          >
            <div className="relative">
              <Input
                id={`${idPrefix}-zipCode`}
                name="zipCode"
                required
                inputMode="numeric"
                autoComplete="postal-code"
                maxLength={9}
                placeholder="00000-000"
                className="pr-9"
                aria-busy={zipCodeLookup.status === "loading"}
                value={address.zipCode}
                onChange={handleZipCodeChange}
              />
              {zipCodeLookup.status === "loading" && (
                <LoaderCircle className="absolute right-3 top-3 size-4 animate-spin text-muted-foreground" aria-hidden />
              )}
            </div>
          </Field>
          <Field label="Logradouro" htmlFor={`${idPrefix}-street`} className="sm:col-span-4">
            <Input ref={streetInput} id={`${idPrefix}-street`} name="street" required maxLength={200} value={address.street} onChange={updateAddress("street")} />
          </Field>
          <Field label="Número" htmlFor={`${idPrefix}-number`} className="sm:col-span-2" hint="Não aparece no site.">
            <Input ref={numberInput} id={`${idPrefix}-number`} name="number" maxLength={20} defaultValue={property?.address.number ?? ""} />
          </Field>
          <Field label="Complemento" htmlFor={`${idPrefix}-complement`} className="sm:col-span-4" hint="Não aparece no site.">
            <Input id={`${idPrefix}-complement`} name="complement" maxLength={100} defaultValue={property?.address.complement ?? ""} />
          </Field>
          <Field label="Bairro" htmlFor={`${idPrefix}-neighborhood`} className="sm:col-span-2">
            <Input id={`${idPrefix}-neighborhood`} name="neighborhood" required maxLength={100} value={address.neighborhood} onChange={updateAddress("neighborhood")} />
          </Field>
          <Field label="Cidade" htmlFor={`${idPrefix}-city`} className="sm:col-span-3">
            <Input id={`${idPrefix}-city`} name="city" required maxLength={100} value={address.city} onChange={updateAddress("city")} />
          </Field>
          <Field label="UF" htmlFor={`${idPrefix}-state`}>
            <Select id={`${idPrefix}-state`} name="state" required value={address.state} onChange={updateAddress("state")}>
              {states.map((state) => (
                <option key={state} value={state}>
                  {state}
                </option>
              ))}
            </Select>
          </Field>
        </div>

        {error && <Alert>{error}</Alert>}
        {success && <Alert tone="success">Dados salvos com sucesso.</Alert>}
        {!readOnly && (
          <Button type="submit" loading={saving} className="self-start">
            <Save className="size-4" aria-hidden /> {submitLabel}
          </Button>
        )}
      </fieldset>
    </form>
  );
}
