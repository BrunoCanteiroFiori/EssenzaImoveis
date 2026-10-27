import type { ReactNode } from "react";
import Link from "next/link";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { buttonClass } from "./button";

type PaginationProps = {
  page: number;
  totalPages: number;
  /** Navegação por links (páginas públicas, renderizadas no servidor). */
  hrefForPage?: (page: number) => string;
  /** Navegação por estado (área administrativa). */
  onPageChange?: (page: number) => void;
};

export function Pagination({ page, totalPages, hrefForPage, onPageChange }: PaginationProps) {
  if (totalPages <= 1) return null;

  const control = (target: number, label: string, icon: ReactNode, disabled: boolean) => {
    const className = buttonClass("secondary", "sm", disabled ? "pointer-events-none opacity-40" : undefined);
    if (hrefForPage) {
      return disabled ? (
        <span className={className} aria-hidden>
          {icon}
        </span>
      ) : (
        <Link href={hrefForPage(target)} className={className} aria-label={label}>
          {icon}
        </Link>
      );
    }
    return (
      <button type="button" className={className} aria-label={label} disabled={disabled} onClick={() => onPageChange?.(target)}>
        {icon}
      </button>
    );
  };

  return (
    <nav aria-label="Paginação" className="flex items-center justify-center gap-3 pt-6">
      {control(page - 1, "Página anterior", <ChevronLeft className="size-4" aria-hidden />, page <= 1)}
      <span className="text-sm text-muted-foreground">
        Página {page} de {totalPages}
      </span>
      {control(page + 1, "Próxima página", <ChevronRight className="size-4" aria-hidden />, page >= totalPages)}
    </nav>
  );
}
