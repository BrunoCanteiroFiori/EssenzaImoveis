"use client";

import { useState } from "react";
import { Maximize2 } from "lucide-react";
import { Panel } from "@/components/admin/page-header";
import { PropertyCard } from "@/components/properties/property-card";
import { PropertyDetailsView } from "@/components/properties/property-details-view";
import { Button } from "@/components/ui/button";
import { Dialog } from "@/components/ui/dialog";
import { cn } from "@/lib/cn";
import { propertyStatusLabels } from "@/lib/labels";
import type { Property, PropertyDetails, PropertySummary } from "@/lib/properties.types";
import { toPropertyInput, type PropertyDraft } from "./properties.draft";

const views = { card: "Card", details: "Página do imóvel" };

type PreviewView = keyof typeof views;

/** Junta o rascunho do formulário às fotos já salvas. Campo vazio vira exemplo, para o layout ficar parecido com o final. */
function toPreview(draft: PropertyDraft, property?: Property): PropertySummary & PropertyDetails {
  const { address, ...input } = toPropertyInput(draft);
  const media = property?.media ?? [];
  return {
    ...input,
    id: property?.id ?? "preview",
    title: input.title.trim() || "Título do anúncio",
    description: input.description.trim() || "A descrição do imóvel aparece aqui.",
    status: property?.status ?? "Available",
    neighborhood: address.neighborhood.trim() || "Bairro",
    city: address.city.trim() || "Cidade",
    state: address.state,
    coverImageUrl: media.find((item) => item.type === "Photo")?.url ?? null,
    media,
    mediaCount: media.length,
    createdAt: property?.createdAt ?? "",
  };
}

/** Prévia em tempo real do anúncio, com os mesmos componentes da área pública. */
export function PropertyPreview({ draft, property }: { draft: PropertyDraft; property?: Property }) {
  const [view, setView] = useState<PreviewView>("card");
  const [expanded, setExpanded] = useState(false);
  const preview = toPreview(draft, property);
  const showMap = Boolean(draft.neighborhood.trim() && draft.city.trim());

  return (
    <Panel title="Prévia no site" className="xl:sticky xl:top-4 xl:max-h-[calc(100vh-2rem)] xl:overflow-y-auto">
      <div className="flex flex-col gap-4">
        <div className="flex items-center justify-between gap-2">
          <div className="flex gap-1 rounded-lg bg-surface-muted p-1">
            {(Object.keys(views) as PreviewView[]).map((option) => (
              <button
                key={option}
                type="button"
                aria-pressed={view === option}
                onClick={() => setView(option)}
                className={cn(
                  "rounded-md px-3 py-1 text-sm font-medium transition-colors",
                  view === option ? "bg-surface text-foreground shadow-sm" : "text-muted-foreground hover:text-foreground",
                )}
              >
                {views[option]}
              </button>
            ))}
          </div>
          {view === "details" && (
            <Button variant="ghost" size="sm" title="Ver em tamanho de computador" onClick={() => setExpanded(true)}>
              <Maximize2 className="size-4" aria-hidden /> Ampliar
            </Button>
          )}
        </div>

        {view === "card" ? <PropertyCard property={preview} preview /> : !expanded && <PropertyDetailsView property={preview} preview showMap={showMap} />}

        {property && property.status !== "Available" && (
          <p className="text-xs text-muted-foreground">
            Status atual: {propertyStatusLabels[property.status]}. Só imóveis disponíveis aparecem no site.
          </p>
        )}
      </div>

      <Dialog open={expanded} title="Prévia da página do imóvel" size="xl" onClose={() => setExpanded(false)}>
        <PropertyDetailsView property={preview} preview showMap={showMap} />
      </Dialog>
    </Panel>
  );
}
