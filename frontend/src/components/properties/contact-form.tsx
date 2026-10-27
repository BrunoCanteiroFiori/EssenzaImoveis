"use client";

import { useState, type FormEvent } from "react";
import { CircleCheck, Send } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Field, Input, Textarea } from "@/components/ui/field";
import { Alert } from "@/components/ui/feedback";
import { ApiError } from "@/lib/api-client";
import { sendContactMessage } from "@/lib/contact-messages.api";

type ContactFormProps = {
  propertyId?: string;
  propertyTitle?: string;
};

/** Mensagem direta para a corretora, que retorna manualmente por e-mail ou telefone. */
export function ContactForm({ propertyId, propertyTitle }: ContactFormProps) {
  const [status, setStatus] = useState<"idle" | "sending" | "sent">("idle");
  const [error, setError] = useState<string | null>(null);
  const idPrefix = propertyId ? `contact-${propertyId}` : "contact";

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    const email = String(data.get("email") ?? "").trim();
    const phone = String(data.get("phone") ?? "").trim();
    if (!email && !phone) {
      setError("Informe um e-mail ou um telefone para retornarmos.");
      return;
    }

    setStatus("sending");
    setError(null);
    try {
      await sendContactMessage({
        propertyId,
        name: String(data.get("name") ?? ""),
        email: email || undefined,
        phone: phone || undefined,
        message: String(data.get("message") ?? ""),
        website: String(data.get("website") ?? "") || undefined,
      });
      form.reset();
      setStatus("sent");
    } catch (exception) {
      setError(exception instanceof ApiError ? exception.message : "Não foi possível enviar. Tente novamente.");
      setStatus("idle");
    }
  }

  if (status === "sent") {
    return (
      <div className="flex flex-col items-center gap-3 rounded-2xl border bg-surface p-6 text-center">
        <CircleCheck className="size-10 text-accent" aria-hidden />
        <p className="font-semibold">Mensagem enviada!</p>
        <p className="text-sm text-muted-foreground">Um corretor vai entrar em contato com você em breve.</p>
        <Button variant="secondary" size="sm" onClick={() => setStatus("idle")}>
          Enviar outra mensagem
        </Button>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      <Field label="Nome" htmlFor={`${idPrefix}-name`}>
        <Input id={`${idPrefix}-name`} name="name" required maxLength={100} autoComplete="name" />
      </Field>
      <div className="grid gap-4 sm:grid-cols-2">
        <Field label="E-mail" htmlFor={`${idPrefix}-email`}>
          <Input id={`${idPrefix}-email`} name="email" type="email" maxLength={200} autoComplete="email" />
        </Field>
        <Field label="Telefone / WhatsApp" htmlFor={`${idPrefix}-phone`}>
          <Input id={`${idPrefix}-phone`} name="phone" type="tel" maxLength={20} autoComplete="tel" placeholder="(11) 99999-9999" />
        </Field>
      </div>
      <Field label="Mensagem" htmlFor={`${idPrefix}-message`}>
        <Textarea
          id={`${idPrefix}-message`}
          name="message"
          required
          maxLength={2000}
          defaultValue={propertyTitle ? `Olá! Tenho interesse no imóvel "${propertyTitle}". Podem me retornar?` : ""}
        />
      </Field>
      <div aria-hidden className="absolute -left-[9999px] h-0 w-0 overflow-hidden">
        <label htmlFor={`${idPrefix}-website`}>Não preencha este campo</label>
        <input id={`${idPrefix}-website`} name="website" tabIndex={-1} autoComplete="off" />
      </div>
      {error && <Alert>{error}</Alert>}
      <Button type="submit" loading={status === "sending"}>
        <Send className="size-4" aria-hidden /> Enviar mensagem
      </Button>
      <p className="text-xs text-muted-foreground">Seus dados são usados apenas para retornarmos o seu contato.</p>
    </form>
  );
}
