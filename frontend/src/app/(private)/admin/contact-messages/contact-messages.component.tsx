"use client";

import { useState, type FormEvent } from "react";
import Link from "next/link";
import { House, Mail, MessageCircle, Phone, Save } from "lucide-react";
import { useAuth } from "@/components/admin/auth-provider";
import { Badge } from "@/components/ui/badge";
import { Button, buttonClass } from "@/components/ui/button";
import { Field, Select, Textarea } from "@/components/ui/field";
import { Alert } from "@/components/ui/feedback";
import {
  contactMessageStatusLabels,
  updateContactMessage,
  type ContactMessage,
  type ContactMessageStatus,
} from "@/lib/contact-messages.api";
import { formatDateTime, formatPhone } from "@/lib/format";
import { whatsappReplyLink } from "@/lib/whatsapp";

const statusTones: Record<ContactMessageStatus, "warning" | "info" | "success" | "neutral"> = {
  New: "warning",
  InProgress: "info",
  Answered: "success",
  Archived: "neutral",
};

/** Uma mensagem recebida: dados do cliente, atalhos para retorno e controle do atendimento. */
export function ContactMessageCard({ contactMessage, onSaved }: { contactMessage: ContactMessage; onSaved: () => void }) {
  const { handleError } = useAuth();
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const firstName = contactMessage.name.split(" ")[0];
  const statuses = (Object.keys(contactMessageStatusLabels) as ContactMessageStatus[]).filter(
    (status) => status !== "New" || contactMessage.status === "New",
  );

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    setSaving(true);
    setError(null);
    try {
      await updateContactMessage(contactMessage.id, String(data.get("status")) as ContactMessageStatus, String(data.get("internalNotes") ?? ""));
      onSaved();
    } catch (exception) {
      setError(handleError(exception));
      setSaving(false);
    }
  }

  return (
    <article className="grid gap-5 rounded-2xl border bg-surface p-5 shadow-sm lg:grid-cols-[1fr_20rem]">
      <div className="flex min-w-0 flex-col gap-3">
        <div className="flex flex-wrap items-center gap-2">
          <h2 className="font-semibold">{contactMessage.name}</h2>
          <Badge tone={statusTones[contactMessage.status]}>{contactMessageStatusLabels[contactMessage.status]}</Badge>
          <span className="text-xs text-muted-foreground">{formatDateTime(contactMessage.createdAt)}</span>
        </div>
        {contactMessage.propertyId && (
          <Link href={`/admin/properties/${contactMessage.propertyId}`} className="inline-flex w-fit items-center gap-1.5 text-sm text-accent hover:underline">
            <House className="size-4" aria-hidden /> {contactMessage.propertyTitle ?? "Imóvel removido"}
          </Link>
        )}
        <p className="whitespace-pre-line rounded-xl bg-surface-muted p-3 text-sm">{contactMessage.message}</p>
        <div className="flex flex-wrap gap-2">
          {contactMessage.phone && (
            <>
              <a
                href={whatsappReplyLink(contactMessage.phone, `Olá, ${firstName}! Recebemos sua mensagem pelo site e estamos à disposição.`)}
                target="_blank"
                rel="noopener noreferrer"
                className={buttonClass("secondary", "sm")}
              >
                <MessageCircle className="size-4 text-[#25D366]" aria-hidden /> WhatsApp
              </a>
              <a href={`tel:+55${contactMessage.phone}`} className={buttonClass("secondary", "sm")}>
                <Phone className="size-4" aria-hidden /> {formatPhone(contactMessage.phone)}
              </a>
            </>
          )}
          {contactMessage.email && (
            <a href={`mailto:${contactMessage.email}`} className={buttonClass("secondary", "sm")}>
              <Mail className="size-4" aria-hidden /> {contactMessage.email}
            </a>
          )}
        </div>
      </div>

      <form onSubmit={handleSubmit} className="flex flex-col gap-3 border-t pt-4 lg:border-l lg:border-t-0 lg:pl-5 lg:pt-0">
        <Field label="Atendimento" htmlFor={`status-${contactMessage.id}`}>
          <Select id={`status-${contactMessage.id}`} name="status" defaultValue={contactMessage.status === "New" ? "InProgress" : contactMessage.status}>
            {statuses.map((status) => (
              <option key={status} value={status}>
                {contactMessageStatusLabels[status]}
              </option>
            ))}
          </Select>
        </Field>
        <Field label="Anotações internas" htmlFor={`notes-${contactMessage.id}`}>
          <Textarea
            id={`notes-${contactMessage.id}`}
            name="internalNotes"
            maxLength={2000}
            rows={3}
            defaultValue={contactMessage.internalNotes ?? ""}
            placeholder="Ex.: retornado por WhatsApp, visita marcada..."
          />
        </Field>
        {error && <Alert>{error}</Alert>}
        <Button type="submit" size="sm" loading={saving}>
          <Save className="size-4" aria-hidden /> Salvar
        </Button>
      </form>
    </article>
  );
}
