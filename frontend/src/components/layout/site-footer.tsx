import Link from "next/link";
import { Lock, MessageCircle } from "lucide-react";
import { siteConfig } from "@/lib/site-config";
import { whatsappLink } from "@/lib/whatsapp";
import { Logo } from "./logo";

export function SiteFooter() {
  const whatsapp = whatsappLink("Olá! Vim pelo site e gostaria de mais informações sobre imóveis.");

  return (
    <footer className="mt-auto border-t bg-surface">
      <div className="mx-auto grid max-w-7xl gap-8 px-4 py-10 sm:px-6 md:grid-cols-3">
        <div className="flex flex-col gap-3">
          <Logo />
          <p className="max-w-xs text-sm text-muted-foreground">
            Compra, venda e aluguel de imóveis com atendimento próximo, do primeiro contato à entrega das chaves.
          </p>
        </div>
        <div className="flex flex-col gap-2 text-sm">
          <p className="font-medium">Imóveis</p>
          <Link href="/properties?purpose=Sale" className="text-muted-foreground hover:text-foreground">
            À venda
          </Link>
          <Link href="/properties?purpose=Rent" className="text-muted-foreground hover:text-foreground">
            Para alugar
          </Link>
          <Link href="/properties?type=Land" className="text-muted-foreground hover:text-foreground">
            Terrenos
          </Link>
        </div>
        <div className="flex flex-col gap-2 text-sm">
          <p className="font-medium">Atendimento</p>
          {whatsapp && (
            <a href={whatsapp} target="_blank" rel="noopener noreferrer" className="inline-flex items-center gap-2 text-muted-foreground hover:text-foreground">
              <MessageCircle className="size-4" aria-hidden /> WhatsApp
            </a>
          )}
          <Link href="/#contato" className="text-muted-foreground hover:text-foreground">
            Enviar mensagem
          </Link>
          <Link href="/auth/login" className="inline-flex items-center gap-2 text-muted-foreground hover:text-foreground">
            <Lock className="size-4" aria-hidden /> Área restrita
          </Link>
        </div>
      </div>
      <p className="border-t py-4 text-center text-xs text-muted-foreground">© {siteConfig.name}. Todos os direitos reservados.</p>
    </footer>
  );
}
