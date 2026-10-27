import type { Metadata } from "next";
import { AuditLogsPage } from "./audit-logs.page";

export const instant = false;

export const metadata: Metadata = { title: "Auditoria" };

export default function Page() {
  return <AuditLogsPage />;
}
