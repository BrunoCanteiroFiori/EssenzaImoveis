"use client";

import { useId, useRef, useState, type ChangeEvent, type Dispatch, type FormEvent, type SetStateAction } from "react";
import { LoaderCircle, Save } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Field, Input, NumericInput, Select, Textarea } from "@/components/ui/field";
import { Alert } from "@/components/ui/feedback";
import { ApiError } from "@/lib/api-client";
import { formatCountInput, formatDecimalInput, formatZipCode } from "@/lib/format";
import { propertyPurposeLabels, propertyPurposes, propertyTypeLabels, propertyTypes } from "@/lib/labels";
import type { PropertyInput } from "@/lib/properties.types";
import { getZipCodeAddress } from "@/lib/zip-codes.api";
import { toPropertyInput, type PropertyDraft } from "./properties.draft";

const states = [
  "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA",
  "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO",
];

type PropertyFormProps = {
  draft: PropertyDraft;
  onDraftChange: Dispatch<SetStateAction<PropertyDraft>>;
  readOnly?: boolean;
  submitLabel: string;
  onSubmit: (input: PropertyInput) => Promise<void>;
};

type AutofilledAddress = Pick<PropertyDraft, "street" | "neighborhood" | "city" | "state">;

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
export function PropertyForm({ draft, onDraftChange, readOnly = false, submitLabel, onSubmit }: PropertyFormProps) {
  const idPrefix = useId();
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [saving, setSaving] = useState(false);
  const [zipCodeLookup, setZipCodeLookup] = useState<ZipCodeLookup>({ status: "idle" });
  const zipCodeRequest = useRef<AbortController | null>(null);
  const autofilledAddress = useRef<AutofilledAddress | null>(null);
  const streetInput = useRef<HTMLInputElement>(null);
  const numberInput = useRef<HTMLInputElement>(null);
  const isLand = draft.type === "Land";

  /** Liga um controle ao rascunho, aplicando a máscara do campo. */
  function bind(field: keyof PropertyDraft, mask: (value: string) => string = (value) => value) {
    return {
      value: draft[field],
      onChange: (event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
        const value = mask(event.target.value);
        onDraftChange((current) => ({ ...current, [field]: value }));
      },
    };
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
      onDraftChange((current) => ({ ...current, ...filled }));
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
        onDraftChange((current) => ({
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
    const changed = digits !== zipCodeDigits(draft.zipCode);
    onDraftChange((current) => ({ ...current, zipCode }));
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
    setSaving(true);
    setError(null);
    setSuccess(false);
    try {
      await onSubmit(toPropertyInput(draft));
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
            <Input id={`${idPrefix}-title`} name="title" required maxLength={150} {...bind("title")} />
          </Field>
          <Field label="Tipo" htmlFor={`${idPrefix}-type`}>
            <Select id={`${idPrefix}-type`} name="type" {...bind("type")}>
              {propertyTypes.map((option) => (
                <option key={option} value={option}>
                  {propertyTypeLabels[option]}
                </option>
              ))}
            </Select>
          </Field>
          <Field label="Finalidade" htmlFor={`${idPrefix}-purpose`}>
            <Select id={`${idPrefix}-purpose`} name="purpose" {...bind("purpose")}>
              {propertyPurposes.map((option) => (
                <option key={option} value={option}>
                  {propertyPurposeLabels[option]}
                </option>
              ))}
            </Select>
          </Field>
          <Field label="Descrição" htmlFor={`${idPrefix}-description`} className="md:col-span-2">
            <Textarea id={`${idPrefix}-description`} name="description" required maxLength={5000} rows={6} {...bind("description")} />
          </Field>
        </div>

        <div className="grid gap-4 sm:grid-cols-3">
          <Field label="Valor" htmlFor={`${idPrefix}-price`} hint="Venda: valor total. Aluguel: valor mensal.">
            <NumericInput id={`${idPrefix}-price`} name="price" required prefix="R$" placeholder="0,00" {...bind("price", formatDecimalInput)} />
          </Field>
          <Field label="Condomínio (mês)" htmlFor={`${idPrefix}-condominiumFee`}>
            <NumericInput id={`${idPrefix}-condominiumFee`} name="condominiumFee" prefix="R$" placeholder="0,00" {...bind("condominiumFee", formatDecimalInput)} />
          </Field>
          <Field label="IPTU (ano)" htmlFor={`${idPrefix}-propertyTax`}>
            <NumericInput id={`${idPrefix}-propertyTax`} name="propertyTax" prefix="R$" placeholder="0,00" {...bind("propertyTax", formatDecimalInput)} />
          </Field>
        </div>

        <div className="grid gap-4 sm:grid-cols-4">
          <Field label="Área" htmlFor={`${idPrefix}-area`}>
            <NumericInput id={`${idPrefix}-area`} name="area" required suffix="m²" placeholder="0,00" {...bind("area", formatDecimalInput)} />
          </Field>
          <Field label="Quartos" htmlFor={`${idPrefix}-bedrooms`} hint={isLand ? "Não se aplica a terrenos." : undefined}>
            <NumericInput id={`${idPrefix}-bedrooms`} name="bedrooms" disabled={isLand} {...bind("bedrooms", formatCountInput)} value={isLand ? "0" : draft.bedrooms} />
          </Field>
          <Field label="Banheiros" htmlFor={`${idPrefix}-bathrooms`}>
            <NumericInput id={`${idPrefix}-bathrooms`} name="bathrooms" disabled={isLand} {...bind("bathrooms", formatCountInput)} value={isLand ? "0" : draft.bathrooms} />
          </Field>
          <Field label="Vagas" htmlFor={`${idPrefix}-parkingSpaces`}>
            <NumericInput id={`${idPrefix}-parkingSpaces`} name="parkingSpaces" {...bind("parkingSpaces", formatCountInput)} />
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
                value={draft.zipCode}
                onChange={handleZipCodeChange}
              />
              {zipCodeLookup.status === "loading" && (
                <LoaderCircle className="absolute right-3 top-3 size-4 animate-spin text-muted-foreground" aria-hidden />
              )}
            </div>
          </Field>
          <Field label="Logradouro" htmlFor={`${idPrefix}-street`} className="sm:col-span-4">
            <Input ref={streetInput} id={`${idPrefix}-street`} name="street" required maxLength={200} {...bind("street")} />
          </Field>
          <Field label="Número" htmlFor={`${idPrefix}-number`} className="sm:col-span-2" hint="Não aparece no site.">
            <Input ref={numberInput} id={`${idPrefix}-number`} name="number" maxLength={20} {...bind("number")} />
          </Field>
          <Field label="Complemento" htmlFor={`${idPrefix}-complement`} className="sm:col-span-4" hint="Não aparece no site.">
            <Input id={`${idPrefix}-complement`} name="complement" maxLength={100} {...bind("complement")} />
          </Field>
          <Field label="Bairro" htmlFor={`${idPrefix}-neighborhood`} className="sm:col-span-2">
            <Input id={`${idPrefix}-neighborhood`} name="neighborhood" required maxLength={100} {...bind("neighborhood")} />
          </Field>
          <Field label="Cidade" htmlFor={`${idPrefix}-city`} className="sm:col-span-3">
            <Input id={`${idPrefix}-city`} name="city" required maxLength={100} {...bind("city")} />
          </Field>
          <Field label="UF" htmlFor={`${idPrefix}-state`}>
            <Select id={`${idPrefix}-state`} name="state" required {...bind("state")}>
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
