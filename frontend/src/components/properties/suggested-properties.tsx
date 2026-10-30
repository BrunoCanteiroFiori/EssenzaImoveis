"use client";

import { useEffect, useState } from "react";
import { Sparkles } from "lucide-react";
import { getPropertySuggestions } from "@/lib/properties.api";
import type { PropertySummary } from "@/lib/properties.types";
import { getVisitorId } from "@/lib/visitor";
import { PropertySection } from "./property-section";

/** Sugestões com base neste imóvel e nos que o visitante já viu. Sem sugestões, não aparece. */
export function SuggestedProperties({ propertyId }: { propertyId: string }) {
  const [suggestions, setSuggestions] = useState<PropertySummary[]>([]);

  useEffect(() => {
    const request = new AbortController();
    getPropertySuggestions(propertyId, getVisitorId(), request.signal)
      .then(setSuggestions)
      .catch(() => undefined);
    return () => request.abort();
  }, [propertyId]);

  return (
    <PropertySection
      title="Imóveis sugeridos"
      description="Selecionados a partir dos imóveis que você viu."
      icon={<Sparkles className="size-5 text-accent" aria-hidden />}
      properties={suggestions}
    />
  );
}
