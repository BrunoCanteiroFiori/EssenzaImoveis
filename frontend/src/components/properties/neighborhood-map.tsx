"use client";

import { useEffect, useState } from "react";

/** Espera a digitação parar antes de recarregar o mapa (na prévia, o bairro muda a cada tecla). */
const reloadDelay = 800;

type NeighborhoodMapProps = {
  neighborhood: string;
  city: string;
  state: string;
};

/** Mapa do Google com o bairro. A área pública só mostra bairro, cidade e UF: rua, número e CEP nunca entram na busca. */
export function NeighborhoodMap({ neighborhood, city, state }: NeighborhoodMapProps) {
  const location = `${neighborhood}, ${city} - ${state}, Brasil`;
  const [mapLocation, setMapLocation] = useState(location);

  useEffect(() => {
    const timeout = setTimeout(() => setMapLocation(location), reloadDelay);
    return () => clearTimeout(timeout);
  }, [location]);

  return (
    <section className="flex flex-col gap-3">
      <h2 className="text-xl font-semibold">Localização</h2>
      <iframe
        title={`Mapa do bairro ${neighborhood}, ${city}`}
        src={`https://www.google.com/maps?q=${encodeURIComponent(mapLocation)}&z=14&hl=pt-BR&output=embed`}
        loading="lazy"
        className="aspect-video w-full rounded-xl border bg-surface-muted"
      />
      <p className="text-sm text-muted-foreground">O mapa mostra o bairro. O endereço exato é informado pelo corretor.</p>
    </section>
  );
}
