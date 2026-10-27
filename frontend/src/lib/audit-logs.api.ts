import { apiFetch, toQueryString } from "./api-client";
import type { PagedResponse } from "./properties.types";

export type AuditAction = "Created" | "Updated" | "Deleted";

export const auditActionLabels: Record<AuditAction, string> = {
  Created: "Inclusão",
  Updated: "Alteração",
  Deleted: "Exclusão",
};

export const auditEntityLabels: Record<string, string> = {
  Property: "Imóvel",
  User: "Usuário",
  ContactMessage: "Mensagem",
};

export type AuditChange = { old: unknown; new: unknown };

export type AuditLog = {
  id: string;
  entityName: string;
  entityId: string;
  action: AuditAction;
  changes: Record<string, unknown>;
  userId: string | null;
  userEmail: string | null;
  occurredAt: string;
};

export type AuditLogFilter = {
  entityName?: string;
  action?: AuditAction;
  from?: string;
  to?: string;
  page: number;
};

export const listAuditLogs = (filter: AuditLogFilter) =>
  apiFetch<PagedResponse<AuditLog>>(`/audit-logs${toQueryString({ ...filter, pageSize: 50 })}`);
