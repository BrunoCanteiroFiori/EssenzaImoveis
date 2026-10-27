import Link from "next/link";
import { House } from "lucide-react";
import { buttonClass } from "@/components/ui/button";

export default function NotFound() {
  return (
    <main className="mx-auto flex max-w-md flex-1 flex-col items-center justify-center gap-4 px-4 py-24 text-center">
      <House className="size-10 text-muted-foreground" aria-hidden />
      <h1 className="text-2xl font-semibold">Página não encontrada</h1>
      <p className="text-muted-foreground">O imóvel ou a página que você procura não existe mais ou foi removido.</p>
      <Link href="/properties" className={buttonClass()}>
        Ver imóveis disponíveis
      </Link>
    </main>
  );
}
