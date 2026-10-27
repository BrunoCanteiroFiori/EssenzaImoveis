"use client";

import { useCallback, useState } from "react";
import { Inbox } from "lucide-react";
import { RequirePermission } from "@/components/admin/admin-shell";
import { PageHeader } from "@/components/admin/page-header";
import { useApiData } from "@/components/admin/use-api-data";
import { Alert, EmptyState, Spinner } from "@/components/ui/feedback";
import { Pagination } from "@/components/ui/pagination";
import { cn } from "@/lib/cn";
import { contactMessageStatusLabels, listContactMessages, type ContactMessageStatus } from "@/lib/contact-messages.api";
import { ContactMessageCard } from "./contact-messages.component";

const tabs: Array<{ value?: ContactMessageStatus; label: string }> = [
  { value: "New", label: contactMessageStatusLabels.New },
  { value: "InProgress", label: contactMessageStatusLabels.InProgress },
  { value: "Answered", label: contactMessageStatusLabels.Answered },
  { value: "Archived", label: contactMessageStatusLabels.Archived },
  { value: undefined, label: "Todas" },
];

export function ContactMessagesPage() {
  return (
    <RequirePermission permission="ManageMessages">
      <ContactMessagesList />
    </RequirePermission>
  );
}

function ContactMessagesList() {
  const [status, setStatus] = useState<ContactMessageStatus | undefined>("New");
  const [page, setPage] = useState(1);
  const load = useCallback(() => listContactMessages(status, page), [status, page]);
  const { data, error, loading, reload } = useApiData(load);

  return (
    <>
      <PageHeader title="Mensagens do site" description="Contatos enviados pelos clientes para retorno manual (WhatsApp, telefone ou e-mail)." />

      <div role="tablist" aria-label="Filtrar por status" className="scrollbar-none mb-4 flex gap-1 overflow-x-auto rounded-xl bg-surface-muted p-1">
        {tabs.map((tab) => (
          <button
            key={tab.label}
            type="button"
            role="tab"
            aria-selected={status === tab.value}
            onClick={() => {
              setStatus(tab.value);
              setPage(1);
            }}
            className={cn(
              "shrink-0 rounded-lg px-4 py-1.5 text-sm font-medium transition-colors",
              status === tab.value ? "bg-surface text-foreground shadow-sm" : "text-muted-foreground hover:text-foreground",
            )}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {error && <Alert className="mb-4">{error}</Alert>}
      {loading ? (
        <Spinner />
      ) : data && data.items.length === 0 ? (
        <EmptyState icon={<Inbox className="size-8" aria-hidden />} title="Nenhuma mensagem aqui." />
      ) : (
        data && (
          <>
            <div className="flex flex-col gap-4">
              {data.items.map((contactMessage) => (
                <ContactMessageCard key={`${contactMessage.id}-${contactMessage.updatedAt}`} contactMessage={contactMessage} onSaved={reload} />
              ))}
            </div>
            <Pagination page={data.page} totalPages={data.totalPages} onPageChange={setPage} />
          </>
        )
      )}
    </>
  );
}
