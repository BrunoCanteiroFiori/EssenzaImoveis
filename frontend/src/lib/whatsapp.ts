import { siteConfig } from "./site-config";

/** Link do WhatsApp da corretora com a mensagem já preenchida (null se o número não estiver configurado). */
export function whatsappLink(message: string): string | null {
  if (!siteConfig.whatsappNumber) return null;
  return `https://wa.me/${siteConfig.whatsappNumber}?text=${encodeURIComponent(message)}`;
}

/** Link para a corretora responder um cliente pelo WhatsApp (telefone brasileiro com DDD). */
export function whatsappReplyLink(phoneDigits: string, message: string) {
  const number = phoneDigits.length <= 11 ? `55${phoneDigits}` : phoneDigits;
  return `https://wa.me/${number}?text=${encodeURIComponent(message)}`;
}
