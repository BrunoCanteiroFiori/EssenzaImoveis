import { MessageCircle } from "lucide-react";
import { whatsappLink } from "@/lib/whatsapp";

/** Botão flutuante de WhatsApp (some se o número não estiver configurado no .env). */
export function WhatsAppButton() {
  const href = whatsappLink("Olá! Vim pelo site e gostaria de falar com um corretor.");
  if (!href) return null;

  return (
    <a
      href={href}
      target="_blank"
      rel="noopener noreferrer"
      aria-label="Falar pelo WhatsApp"
      className="fixed bottom-5 right-5 z-40 flex size-14 items-center justify-center rounded-full bg-[#25D366] text-white shadow-lg transition-transform hover:scale-105"
    >
      <MessageCircle className="size-7" aria-hidden />
    </a>
  );
}
