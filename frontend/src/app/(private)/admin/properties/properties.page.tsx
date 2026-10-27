"use client";

import { useCallback, useState, type FormEvent } from "react";
import Link from "next/link";
import { Plus, Search } from "lucide-react";
import { RequirePermission } from "@/components/admin/admin-shell";
import { useAuth } from "@/components/admin/auth-provider";
import { PageHeader } from "@/components/admin/page-header";
import { useApiData } from "@/components/admin/use-api-data";
import { Button, buttonClass } from "@/components/ui/button";
import { Input, Select } from "@/components/ui/field";
import { Alert, Spinner } from "@/components/ui/feedback";
import { Pagination } from "@/components/ui/pagination";
import {
  propertyPurposeLabels,
  propertyPurposes,
  propertyStatusLabels,
  propertyStatuses,
  propertyTypeLabels,
  propertyTypes,
  sortOrderLabels,
} from "@/lib/labels";
import { listProperties, type PropertyListFilter } from "@/lib/properties.api";
import type { PropertyPurpose, PropertySortOrder, PropertyStatus, PropertyType } from "@/lib/properties.types";
import { PropertiesComponent } from "./properties.component";

const adminSortOrders: PropertySortOrder[] = ["Newest", "Hottest", "PriceAscending", "PriceDescending"];

export function PropertiesPage() {
  return (
    <RequirePermission permission="ReadProperties">
      <PropertiesList />
    </RequirePermission>
  );
}

function PropertiesList() {
  const { hasPermission } = useAuth();
  const [filter, setFilter] = useState<PropertyListFilter>({ sort: "Newest", page: 1 });
  const load = useCallback(() => listProperties(filter), [filter]);
  const { data, error, loading, reload } = useApiData(load);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setFilter({
      search: String(form.get("search") ?? "").trim() || undefined,
      status: (String(form.get("status") ?? "") || undefined) as PropertyStatus | undefined,
      type: (String(form.get("type") ?? "") || undefined) as PropertyType | undefined,
      purpose: (String(form.get("purpose") ?? "") || undefined) as PropertyPurpose | undefined,
      sort: String(form.get("sort") ?? "Newest") as PropertySortOrder,
      page: 1,
    });
  }

  return (
    <>
      <PageHeader
        title="Imóveis"
        description="Cadastre, edite, patrocine e acompanhe a temperatura dos anúncios."
        actions={
          hasPermission("WriteProperties") && (
            <Link href="/admin/properties/new" className={buttonClass()}>
              <Plus className="size-4" aria-hidden /> Novo imóvel
            </Link>
          )
        }
      />

      <form onSubmit={handleSubmit} className="mb-4 grid gap-3 rounded-2xl border bg-surface p-4 sm:grid-cols-2 lg:grid-cols-3 2xl:grid-cols-[1fr_repeat(4,10rem)_auto]">
        <Input name="search" placeholder="Título, bairro ou cidade" aria-label="Buscar" defaultValue={filter.search} />
        <Select name="status" aria-label="Status" defaultValue={filter.status ?? ""}>
          <option value="">Todos os status</option>
          {propertyStatuses.map((status) => (
            <option key={status} value={status}>
              {propertyStatusLabels[status]}
            </option>
          ))}
        </Select>
        <Select name="type" aria-label="Tipo" defaultValue={filter.type ?? ""}>
          <option value="">Todos os tipos</option>
          {propertyTypes.map((type) => (
            <option key={type} value={type}>
              {propertyTypeLabels[type]}
            </option>
          ))}
        </Select>
        <Select name="purpose" aria-label="Finalidade" defaultValue={filter.purpose ?? ""}>
          <option value="">Venda e aluguel</option>
          {propertyPurposes.map((purpose) => (
            <option key={purpose} value={purpose}>
              {propertyPurposeLabels[purpose]}
            </option>
          ))}
        </Select>
        <Select name="sort" aria-label="Ordenação" defaultValue={filter.sort}>
          {adminSortOrders.map((sort) => (
            <option key={sort} value={sort}>
              {sortOrderLabels[sort]}
            </option>
          ))}
        </Select>
        <Button type="submit" variant="secondary">
          <Search className="size-4" aria-hidden /> Filtrar
        </Button>
      </form>

      {error && <Alert className="mb-4">{error}</Alert>}
      {loading ? (
        <Spinner />
      ) : (
        data && (
          <>
            <p className="mb-2 text-sm text-muted-foreground">{data.totalCount} imóvel(is)</p>
            <PropertiesComponent properties={data.items} onChanged={reload} />
            <Pagination page={data.page} totalPages={data.totalPages} onPageChange={(page) => setFilter((current) => ({ ...current, page }))} />
          </>
        )
      )}
    </>
  );
}
