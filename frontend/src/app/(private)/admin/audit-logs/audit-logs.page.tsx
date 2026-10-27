"use client";

import { useCallback, useState, type FormEvent } from "react";
import { Lock, Search } from "lucide-react";
import { RequirePermission } from "@/components/admin/admin-shell";
import { PageHeader } from "@/components/admin/page-header";
import { useApiData } from "@/components/admin/use-api-data";
import { Button } from "@/components/ui/button";
import { Field, Input, Select } from "@/components/ui/field";
import { Alert, EmptyState, Spinner } from "@/components/ui/feedback";
import { Pagination } from "@/components/ui/pagination";
import { auditActionLabels, auditEntityLabels, listAuditLogs, type AuditAction, type AuditLogFilter } from "@/lib/audit-logs.api";
import { AuditLogsComponent } from "./audit-logs.component";

/** Datas do filtro no fuso de Brasília. */
const brasiliaOffset = "-03:00";

export function AuditLogsPage() {
  return (
    <RequirePermission superAdminOnly>
      <AuditLogsList />
    </RequirePermission>
  );
}

function AuditLogsList() {
  const [filter, setFilter] = useState<AuditLogFilter>({ page: 1 });
  const load = useCallback(() => listAuditLogs(filter), [filter]);
  const { data, error, loading } = useApiData(load);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const from = String(form.get("from") ?? "");
    const to = String(form.get("to") ?? "");
    setFilter({
      entityName: String(form.get("entityName") ?? "") || undefined,
      action: (String(form.get("action") ?? "") || undefined) as AuditAction | undefined,
      from: from ? `${from}T00:00:00${brasiliaOffset}` : undefined,
      to: to ? `${to}T23:59:59${brasiliaOffset}` : undefined,
      page: 1,
    });
  }

  return (
    <>
      <PageHeader
        title="Auditoria"
        description="Todas as inclusões, alterações e exclusões feitas no sistema, com autor e data."
        actions={
          <span className="inline-flex items-center gap-1.5 rounded-full bg-surface-muted px-3 py-1 text-xs font-medium text-muted-foreground">
            <Lock className="size-3.5" aria-hidden /> Somente leitura
          </span>
        }
      />

      <form onSubmit={handleSubmit} className="mb-4 grid gap-3 rounded-2xl border bg-surface p-4 sm:grid-cols-2 xl:grid-cols-[repeat(4,1fr)_auto]">
        <Field label="Registro" htmlFor="audit-entity">
          <Select id="audit-entity" name="entityName" defaultValue="">
            <option value="">Todos</option>
            {Object.entries(auditEntityLabels).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </Select>
        </Field>
        <Field label="Ação" htmlFor="audit-action">
          <Select id="audit-action" name="action" defaultValue="">
            <option value="">Todas</option>
            {Object.entries(auditActionLabels).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </Select>
        </Field>
        <Field label="De" htmlFor="audit-from">
          <Input id="audit-from" name="from" type="date" />
        </Field>
        <Field label="Até" htmlFor="audit-to">
          <Input id="audit-to" name="to" type="date" />
        </Field>
        <Button type="submit" variant="secondary" className="self-end">
          <Search className="size-4" aria-hidden /> Filtrar
        </Button>
      </form>

      {error && <Alert className="mb-4">{error}</Alert>}
      {loading ? (
        <Spinner />
      ) : data && data.items.length === 0 ? (
        <EmptyState title="Nenhum registro encontrado." />
      ) : (
        data && (
          <>
            <p className="mb-2 text-sm text-muted-foreground">{data.totalCount} registro(s)</p>
            <AuditLogsComponent auditLogs={data.items} />
            <Pagination page={data.page} totalPages={data.totalPages} onPageChange={(page) => setFilter((current) => ({ ...current, page }))} />
          </>
        )
      )}
    </>
  );
}
