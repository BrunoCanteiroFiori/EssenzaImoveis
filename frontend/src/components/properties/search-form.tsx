"use client";

import { useEffect, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { Search, SlidersHorizontal } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Field, Input, Select } from "@/components/ui/field";
import { toQueryString } from "@/lib/api-client";
import { cn } from "@/lib/cn";
import { propertyTypeLabels, propertyTypes, sortOrderLabels, sortOrders } from "@/lib/labels";
import { getPropertyLocations } from "@/lib/properties.api";
import type { PropertyLocation, PropertySearch } from "@/lib/properties.types";

type SearchFormProps = {
  initial?: PropertySearch;
  variant?: "hero" | "sidebar";
};

const minimumOptions = [1, 2, 3, 4];

function locationSuggestions(locations: PropertyLocation[]) {
  const suggestions = new Map<string, string>();
  for (const location of locations) {
    suggestions.set(location.city, `Cidade · ${location.state}`);
    suggestions.set(location.neighborhood, `Bairro · ${location.city}`);
  }
  return [...suggestions];
}

function numberOrUndefined(value: FormDataEntryValue | null) {
  const text = String(value ?? "").trim();
  return text === "" ? undefined : Number(text);
}

/** Busca por localidade ou tipo, com filtros de finalidade, quartos, vagas, metragem e valor. */
export function SearchForm({ initial = {}, variant = "hero" }: SearchFormProps) {
  const router = useRouter();
  const [locations, setLocations] = useState<PropertyLocation[]>([]);
  const [showMore, setShowMore] = useState(variant === "sidebar");
  const isHero = variant === "hero";

  useEffect(() => {
    getPropertyLocations()
      .then(setLocations)
      .catch(() => setLocations([]));
  }, []);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const query = toQueryString({
      location: String(data.get("location") ?? "").trim(),
      type: String(data.get("type") ?? ""),
      purpose: String(data.get("purpose") ?? ""),
      minBedrooms: numberOrUndefined(data.get("minBedrooms")),
      minBathrooms: numberOrUndefined(data.get("minBathrooms")),
      minParkingSpaces: numberOrUndefined(data.get("minParkingSpaces")),
      minArea: numberOrUndefined(data.get("minArea")),
      maxArea: numberOrUndefined(data.get("maxArea")),
      minPrice: numberOrUndefined(data.get("minPrice")),
      maxPrice: numberOrUndefined(data.get("maxPrice")),
      sort: String(data.get("sort") ?? "") === "Relevance" ? "" : String(data.get("sort") ?? ""),
    });
    router.push(`/properties${query}`);
  }

  const purposeOptions = [
    { value: "", label: "Todos" },
    { value: "Sale", label: "Comprar" },
    { value: "Rent", label: "Alugar" },
  ];

  return (
    <form
      onSubmit={handleSubmit}
      role="search"
      aria-label="Buscar imóveis"
      className={cn("flex flex-col gap-4", isHero ? "rounded-2xl border bg-surface p-4 shadow-lg sm:p-5" : "")}
    >
      <fieldset className="flex gap-1 rounded-xl bg-surface-muted p-1">
        <legend className="sr-only">Finalidade</legend>
        {purposeOptions.map((option) => (
          <label key={option.value} className="flex-1">
            <input type="radio" name="purpose" value={option.value} defaultChecked={(initial.purpose ?? "") === option.value} className="peer sr-only" />
            <span className="block cursor-pointer rounded-lg px-3 py-1.5 text-center text-sm font-medium text-muted-foreground transition-colors peer-checked:bg-surface peer-checked:text-foreground peer-checked:shadow-sm peer-focus-visible:ring-2 peer-focus-visible:ring-ring">
              {option.label}
            </span>
          </label>
        ))}
      </fieldset>

      <div className={cn("grid gap-3", isHero && "md:grid-cols-[1fr_14rem_auto]")}>
        <Field label="Localidade" htmlFor={`${variant}-location`}>
          <Input
            id={`${variant}-location`}
            name="location"
            list={`${variant}-locations`}
            placeholder="Cidade ou bairro"
            defaultValue={initial.location ?? ""}
            autoComplete="off"
            maxLength={100}
          />
          <datalist id={`${variant}-locations`}>
            {locationSuggestions(locations).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </datalist>
        </Field>
        <Field label="Tipo de imóvel" htmlFor={`${variant}-type`}>
          <Select id={`${variant}-type`} name="type" defaultValue={initial.type ?? ""}>
            <option value="">Todos os tipos</option>
            {propertyTypes.map((type) => (
              <option key={type} value={type}>
                {propertyTypeLabels[type]}
              </option>
            ))}
          </Select>
        </Field>
        {isHero && (
          <Button type="submit" size="lg" className="self-end">
            <Search className="size-4" aria-hidden /> Buscar
          </Button>
        )}
      </div>

      {isHero && (
        <button
          type="button"
          onClick={() => setShowMore((current) => !current)}
          className="inline-flex items-center gap-1.5 self-start text-sm font-medium text-accent hover:underline"
          aria-expanded={showMore}
        >
          <SlidersHorizontal className="size-4" aria-hidden />
          {showMore ? "Menos filtros" : "Mais filtros"}
        </button>
      )}

      <div className={cn("grid gap-3", isHero ? "sm:grid-cols-3 lg:grid-cols-6" : "grid-cols-2", !showMore && "hidden")}>
        <Field label="Quartos" htmlFor={`${variant}-bedrooms`}>
          <Select id={`${variant}-bedrooms`} name="minBedrooms" defaultValue={initial.minBedrooms ?? ""}>
            <option value="">Qualquer</option>
            {minimumOptions.map((value) => (
              <option key={value} value={value}>
                {value}+
              </option>
            ))}
          </Select>
        </Field>
        <Field label="Banheiros" htmlFor={`${variant}-bathrooms`}>
          <Select id={`${variant}-bathrooms`} name="minBathrooms" defaultValue={initial.minBathrooms ?? ""}>
            <option value="">Qualquer</option>
            {minimumOptions.map((value) => (
              <option key={value} value={value}>
                {value}+
              </option>
            ))}
          </Select>
        </Field>
        <Field label="Vagas" htmlFor={`${variant}-parking`} className={isHero ? "" : "col-span-2"}>
          <Select id={`${variant}-parking`} name="minParkingSpaces" defaultValue={initial.minParkingSpaces ?? ""}>
            <option value="">Qualquer</option>
            {minimumOptions.map((value) => (
              <option key={value} value={value}>
                {value}+
              </option>
            ))}
          </Select>
        </Field>
        <Field label="Área mín. (m²)" htmlFor={`${variant}-min-area`}>
          <Input id={`${variant}-min-area`} name="minArea" type="number" min={0} inputMode="numeric" defaultValue={initial.minArea ?? ""} />
        </Field>
        <Field label="Área máx. (m²)" htmlFor={`${variant}-max-area`}>
          <Input id={`${variant}-max-area`} name="maxArea" type="number" min={0} inputMode="numeric" defaultValue={initial.maxArea ?? ""} />
        </Field>
        <div className={isHero ? "hidden lg:block" : "hidden"} />
        <Field label="Valor mín. (R$)" htmlFor={`${variant}-min-price`}>
          <Input id={`${variant}-min-price`} name="minPrice" type="number" min={0} step={1000} inputMode="numeric" defaultValue={initial.minPrice ?? ""} />
        </Field>
        <Field label="Valor máx. (R$)" htmlFor={`${variant}-max-price`}>
          <Input id={`${variant}-max-price`} name="maxPrice" type="number" min={0} step={1000} inputMode="numeric" defaultValue={initial.maxPrice ?? ""} />
        </Field>
        {!isHero && (
          <Field label="Ordenar por" htmlFor={`${variant}-sort`} className="col-span-2">
            <Select id={`${variant}-sort`} name="sort" defaultValue={initial.sort ?? "Relevance"}>
              {sortOrders.map((sort) => (
                <option key={sort} value={sort}>
                  {sortOrderLabels[sort]}
                </option>
              ))}
            </Select>
          </Field>
        )}
      </div>

      {!isHero && (
        <Button type="submit">
          <Search className="size-4" aria-hidden /> Aplicar filtros
        </Button>
      )}
    </form>
  );
}
