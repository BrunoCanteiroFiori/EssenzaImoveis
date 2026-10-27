import type { Metadata } from "next";
import { ContactMessagesPage } from "./contact-messages.page";

export const instant = false;

export const metadata: Metadata = { title: "Mensagens" };

export default function Page() {
  return <ContactMessagesPage />;
}
