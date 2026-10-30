"use client";

import { useEffect } from "react";
import { registerPropertyView } from "@/lib/properties.api";
import { getVisitorId } from "@/lib/visitor";

/** Último imóvel registrado nesta aba, para não repetir a chamada ao renderizar de novo. Voltar a um imóvel anterior registra de novo. */
let lastTrackedProperty: string | null = null;

/** Registra a visita ao imóvel (esquenta o imóvel e entra em "Vistos por último"). A API conta cada visitante uma vez por dia. */
export function ViewTracker({ propertyId }: { propertyId: string }) {
  useEffect(() => {
    if (lastTrackedProperty === propertyId) return;
    lastTrackedProperty = propertyId;
    registerPropertyView(propertyId, getVisitorId()).catch(() => {
      lastTrackedProperty = null;
    });
  }, [propertyId]);

  return null;
}
