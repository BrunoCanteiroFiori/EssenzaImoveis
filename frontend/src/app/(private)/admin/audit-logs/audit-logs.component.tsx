"use client";

import { Fragment, useState } from "react";
import { ChevronDown } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { auditActionLabels, auditEntityLabels, type AuditAction, type AuditChange, type AuditLog } from "@/lib/audit-logs.api";
import { cn } from "@/lib/cn";
import { formatDateTime } from "@/lib/format";

const actionTones: Record<AuditAction, "success" | "info" | "danger"> = {
  Created: "success",
  Updated: "info",
  Deleted: "danger",
};

function formatValue(value: unknown) {
  if (value === null || value === undefined || value === "") return "—";
  if (typeof value === "object") return JSON.stringify(value);
  return String(value);
}

function isChange(value: unknown): value is AuditChange {
  return typeof value === "object" && value !== null && "old" in value && "new" in value;
}

/** Tabela de registros de auditoria; cada linha expande para mostrar os campos alterados. */
export function AuditLogsComponent({ auditLogs }: { auditLogs: AuditLog[] }) {
  const [expanded, setExpanded] = useState<string | null>(null);

  return (
    <div className="overflow-x-auto rounded-2xl border bg-surface">
      <table className="w-full min-w-[48rem] text-sm">
        <thead className="border-b bg-surface-muted text-left text-xs uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-3 font-medium">Data</th>
            <th className="px-4 py-3 font-medium">Autor</th>
            <th className="px-4 py-3 font-medium">Ação</th>
            <th className="px-4 py-3 font-medium">Registro</th>
            <th className="px-4 py-3" />
          </tr>
        </thead>
        <tbody className="divide-y">
          {auditLogs.map((auditLog) => {
            const isOpen = expanded === auditLog.id;
            return (
              <Fragment key={auditLog.id}>
                <tr className="hover:bg-surface-muted/50">
                  <td className="whitespace-nowrap px-4 py-3">{formatDateTime(auditLog.occurredAt)}</td>
                  <td className="px-4 py-3">{auditLog.userEmail ?? <span className="text-muted-foreground">Visitante / sistema</span>}</td>
                  <td className="px-4 py-3">
                    <Badge tone={actionTones[auditLog.action]}>{auditActionLabels[auditLog.action]}</Badge>
                  </td>
                  <td className="px-4 py-3">
                    {auditEntityLabels[auditLog.entityName] ?? auditLog.entityName}
                    <span className="ml-2 font-mono text-xs text-muted-foreground">{auditLog.entityId.slice(0, 8)}</span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <button
                      type="button"
                      onClick={() => setExpanded(isOpen ? null : auditLog.id)}
                      aria-expanded={isOpen}
                      className="inline-flex items-center gap-1 rounded-lg px-2 py-1 text-xs font-medium text-accent hover:bg-accent-soft"
                    >
                      Detalhes <ChevronDown className={cn("size-4 transition-transform", isOpen && "rotate-180")} aria-hidden />
                    </button>
                  </td>
                </tr>
                {isOpen && (
                  <tr>
                    <td colSpan={5} className="bg-surface-muted/40 px-4 py-4">
                      <ChangesTable changes={auditLog.changes} />
                    </td>
                  </tr>
                )}
              </Fragment>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

function ChangesTable({ changes }: { changes: Record<string, unknown> }) {
  const entries = Object.entries(changes);
  const hasDiff = entries.some(([, value]) => isChange(value));

  return (
    <table className="w-full text-xs">
      <thead className="text-left text-muted-foreground">
        <tr>
          <th className="py-1 pr-4 font-medium">Campo</th>
          {hasDiff ? (
            <>
              <th className="py-1 pr-4 font-medium">Antes</th>
              <th className="py-1 font-medium">Depois</th>
            </>
          ) : (
            <th className="py-1 font-medium">Valor</th>
          )}
        </tr>
      </thead>
      <tbody>
        {entries.map(([field, value]) => (
          <tr key={field} className="align-top">
            <td className="py-1 pr-4 font-mono">{field}</td>
            {isChange(value) ? (
              <>
                <td className="max-w-xs break-words py-1 pr-4 text-danger line-through decoration-danger/40">{formatValue(value.old)}</td>
                <td className="max-w-xs break-words py-1 text-accent">{formatValue(value.new)}</td>
              </>
            ) : (
              <td className="max-w-md break-words py-1">{formatValue(value)}</td>
            )}
          </tr>
        ))}
      </tbody>
    </table>
  );
}
