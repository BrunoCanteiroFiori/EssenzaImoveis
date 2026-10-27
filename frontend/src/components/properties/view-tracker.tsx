"use client";

import { useEffect } from "react";
import { registerPropertyView } from "@/lib/properties.api";
import { getVisitorId } from "@/lib/visitor";

/** Imóveis já registrados nesta aba, para não repetir a chamada ao reabrir a página ou renderizar de novo. */
const trackedProperties = new Set<string>();

/** Registra a visita ao imóvel (esquenta o imóvel). A API conta cada visitante uma vez por dia. */
export function ViewTracker({ propertyId }: { propertyId: string }) {
  useEffect(() => {
    if (trackedProperties.has(propertyId)) return;
    trackedProperties.add(propertyId);
    registerPropertyView(propertyId, getVisitorId()).catch(() => trackedProperties.delete(propertyId));
  }, [propertyId]);

  return null;
}
