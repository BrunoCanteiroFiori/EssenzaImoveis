import Link from "next/link";
import { Menu } from "lucide-react";
import { Logo } from "./logo";
import { ThemeToggle } from "./theme-toggle";

const links = [
  { href: "/properties?purpose=Sale", label: "Comprar" },
  { href: "/properties?purpose=Rent", label: "Alugar" },
  { href: "/properties?sort=Hottest", label: "Mais procurados" },
  { href: "/#contato", label: "Contato" },
];

export function SiteHeader() {
  return (
    <header className="sticky top-0 z-40 border-b bg-surface/85 backdrop-blur supports-[backdrop-filter]:bg-surface/70">
      <div className="mx-auto flex h-16 max-w-7xl items-center justify-between gap-4 px-4 sm:px-6">
        <Logo />
        <nav aria-label="Principal" className="hidden items-center gap-1 md:flex">
          {links.map((link) => (
            <Link
              key={link.href}
              href={link.href}
              className="rounded-lg px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-surface-muted hover:text-foreground"
            >
              {link.label}
            </Link>
          ))}
        </nav>
        <div className="flex items-center gap-1">
          <ThemeToggle />
          <details className="relative md:hidden">
            <summary
              className="flex size-9 cursor-pointer list-none items-center justify-center rounded-lg text-muted-foreground hover:bg-surface-muted [&::-webkit-details-marker]:hidden"
              aria-label="Abrir menu"
            >
              <Menu className="size-5" aria-hidden />
            </summary>
            <nav aria-label="Menu" className="absolute right-0 mt-2 flex w-48 flex-col rounded-xl border bg-surface p-2 shadow-lg">
              {links.map((link) => (
                <Link key={link.href} href={link.href} className="rounded-lg px-3 py-2 text-sm hover:bg-surface-muted">
                  {link.label}
                </Link>
              ))}
            </nav>
          </details>
        </div>
      </div>
    </header>
  );
}
