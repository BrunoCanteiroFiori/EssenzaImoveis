"use client";

import { useEffect, useState } from "react";
import { Heart, History } from "lucide-react";
import { getRecentlyViewedProperties } from "@/lib/properties.api";
import type { PropertySummary } from "@/lib/properties.types";
import { loadSavedProperties, useSavedProperties } from "@/lib/saved-properties.store";
import { getVisitorId } from "@/lib/visitor";
import { PropertySection } from "./property-section";

/** Seções pessoais da página inicial, guardadas por navegador. Quem ainda não viu nem salvou nada não vê nenhuma delas. */
export function VisitorSections() {
  const [recentlyViewed, setRecentlyViewed] = useState<PropertySummary[]>([]);
  const { items: saved } = useSavedProperties();

  useEffect(() => {
    getRecentlyViewedProperties(getVisitorId())
      .then(setRecentlyViewed)
      .catch(() => setRecentlyViewed([]));
    void loadSavedProperties();
  }, []);

  if (recentlyViewed.length === 0 && saved.length === 0) return null;

  return (
    <div className="mx-auto flex max-w-7xl flex-col gap-14 px-4 pt-14 sm:px-6">
      <PropertySection
        title="Vistos por último"
        description="Continue de onde você parou."
        icon={<History className="size-5 text-accent" aria-hidden />}
        properties={recentlyViewed}
      />
      <PropertySection
        title="Salvos"
        description="Os imóveis que você guardou para ver depois."
        icon={<Heart className="size-5 fill-danger text-danger" aria-hidden />}
        properties={saved}
      />
    </div>
  );
}
