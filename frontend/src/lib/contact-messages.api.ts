import { apiFetch, toQueryString } from "./api-client";
import type { PagedResponse } from "./properties.types";

export type ContactMessageStatus = "New" | "InProgress" | "Answered" | "Archived";

export const contactMessageStatusLabels: Record<ContactMessageStatus, string> = {
  New: "Nova",
  InProgress: "Em atendimento",
  Answered: "Respondida",
  Archived: "Arquivada",
};

export type ContactMessage = {
  id: string;
  propertyId: string | null;
  propertyTitle: string | null;
  name: string;
  email: string | null;
  phone: string | null;
  message: string;
  status: ContactMessageStatus;
  internalNotes: string | null;
  createdAt: string;
  updatedAt: string;
};

export type ContactMessageInput = {
  propertyId?: string;
  name: string;
  email?: string;
  phone?: string;
  message: string;
  website?: string;
};

export const sendContactMessage = (input: ContactMessageInput) =>
  apiFetch<void>("/contact-messages", { method: "POST", body: input });

export const listContactMessages = (status: ContactMessageStatus | undefined, page: number) =>
  apiFetch<PagedResponse<ContactMessage>>(`/contact-messages${toQueryString({ status, page, pageSize: 20 })}`);

export const updateContactMessage = (id: string, status: ContactMessageStatus, internalNotes: string) =>
  apiFetch<ContactMessage>(`/contact-messages/${id}`, { method: "PUT", body: { status, internalNotes } });
