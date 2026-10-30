"use client";

import { useTransition } from "react";
import { Heart } from "lucide-react";
import { buttonClass } from "@/components/ui/button";
import { cn } from "@/lib/cn";
import { toggleSavedProperty, useSavedProperties } from "@/lib/saved-properties.store";

type SaveButtonProps = {
  propertyId: string;
  /** Botão com texto (página do imóvel) em vez de só o coração (card). */
  labeled?: boolean;
  className?: string;
};

/** Salva o imóvel para este navegador. Os salvos aparecem na página inicial. */
export function SaveButton({ propertyId, labeled = false, className }: SaveButtonProps) {
  const { ids } = useSavedProperties();
  const [pending, startTransition] = useTransition();
  const saved = ids.has(propertyId);

  return (
    <button
      type="button"
      disabled={pending}
      aria-pressed={saved}
      aria-label={labeled ? undefined : "Salvar imóvel"}
      title={saved ? "Remover dos salvos" : "Salvar imóvel"}
      onClick={() => startTransition(() => toggleSavedProperty(propertyId))}
      className={cn(
        labeled
          ? buttonClass("secondary", "sm")
          : "inline-flex size-9 items-center justify-center rounded-full bg-surface/90 text-foreground shadow-sm backdrop-blur transition-colors hover:bg-surface",
        className,
      )}
    >
      <Heart className={cn("size-5", saved && "fill-danger text-danger", labeled && "size-4")} aria-hidden />
      {labeled && (saved ? "Salvo" : "Salvar")}
    </button>
  );
}
