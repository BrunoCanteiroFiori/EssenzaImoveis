import Link from "next/link";
import { SearchX } from "lucide-react";
import { PropertyCard } from "@/components/properties/property-card";
import { SearchForm } from "@/components/properties/search-form";
import { buttonClass } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/feedback";
import { Pagination } from "@/components/ui/pagination";
import { toQueryString } from "@/lib/api-client";
import { pluralize } from "@/lib/format";
import { propertyPurposes, propertyTypes, sortOrders } from "@/lib/labels";
import { searchProperties } from "@/lib/properties.api";
import type { PropertySearch } from "@/lib/properties.types";

export type SearchParams = Record<string, string | string[] | undefined>;

function single(value: string | string[] | undefined) {
  return Array.isArray(value) ? value[0] : value;
}

function positiveNumber(value: string | string[] | undefined) {
  const number = Number(single(value));
  return Number.isFinite(number) && number > 0 ? number : undefined;
}

function oneOf<T extends string>(value: string | string[] | undefined, options: readonly T[]) {
  const text = single(value);
  return options.find((option) => option === text);
}

/** Converte a query string da URL em filtros válidos para a API (valores inválidos são ignorados). */
export function parseSearch(params: SearchParams): PropertySearch {
  return {
    location: single(params.location)?.slice(0, 100) || undefined,
    type: oneOf(params.type, propertyTypes),
    purpose: oneOf(params.purpose, propertyPurposes),
    minBedrooms: positiveNumber(params.minBedrooms),
    minBathrooms: positiveNumber(params.minBathrooms),
    minParkingSpaces: positiveNumber(params.minParkingSpaces),
    minArea: positiveNumber(params.minArea),
    maxArea: positiveNumber(params.maxArea),
    minPrice: positiveNumber(params.minPrice),
    maxPrice: positiveNumber(params.maxPrice),
    sort: oneOf(params.sort, sortOrders),
    page: positiveNumber(params.page),
  };
}

export async function PropertiesComponent({ searchParams }: { searchParams: Promise<SearchParams> }) {
  const search = parseSearch(await searchParams);
  const result = await searchProperties(search);
  const formKey = toQueryString({ ...search, page: undefined });

  return (
    <div className="grid gap-8 lg:grid-cols-[18rem_1fr]">
      <aside className="h-fit rounded-2xl border bg-surface p-5 lg:sticky lg:top-20">
        <h2 className="mb-4 font-semibold">Filtros</h2>
        <SearchForm key={formKey} initial={search} variant="sidebar" />
      </aside>

      <section aria-live="polite" className="flex flex-col gap-4">
        {!result ? (
          <EmptyState title="Não foi possível carregar os imóveis agora.">Tente novamente em alguns instantes.</EmptyState>
        ) : result.items.length === 0 ? (
          <EmptyState icon={<SearchX className="size-8" aria-hidden />} title="Nenhum imóvel encontrado com esses filtros.">
            <p>Tente ampliar a busca ou remover alguns filtros.</p>
            <Link href="/properties" className={buttonClass("secondary", "sm", "mt-4")}>
              Limpar filtros
            </Link>
          </EmptyState>
        ) : (
          <>
            <p className="text-sm text-muted-foreground">{pluralize(result.totalCount, "imóvel encontrado", "imóveis encontrados")}</p>
            <div className="grid gap-6 sm:grid-cols-2 xl:grid-cols-3">
              {result.items.map((property, index) => (
                <PropertyCard key={property.id} property={property} priority={index < 3} />
              ))}
            </div>
            <Pagination
              page={result.page}
              totalPages={result.totalPages}
              hrefForPage={(page) => `/properties${toQueryString({ ...search, page: page > 1 ? page : undefined })}`}
            />
          </>
        )}
      </section>
    </div>
  );
}
