import Image from "next/image";
import Link from "next/link";
import { ImageOff, MapPin } from "lucide-react";
import { cn } from "@/lib/cn";
import { formatPrice } from "@/lib/format";
import { propertyPurposeLabels, propertyTypeLabels } from "@/lib/labels";
import type { PropertySummary } from "@/lib/properties.types";
import { assetUrl } from "@/lib/site-config";
import { PropertySpecs } from "./property-specs";
import { SaveButton } from "./save-button";

type PropertyCardProps = {
  property: PropertySummary;
  priority?: boolean;
  /** Prévia da área administrativa: mesmo visual, com os links desativados. */
  preview?: boolean;
};

/**
 * Na área pública, só aparece imóvel fora de "Disponível" entre os salvos do visitante: o card fica sem link, porque a página
 * do imóvel não existe mais para o público, mas o coração continua ativo para tirá-lo da lista.
 */
export function PropertyCard({ property, priority = false, preview = false }: PropertyCardProps) {
  const cover = assetUrl(property.coverImageUrl);
  const unavailable = !preview && property.status !== "Available";
  const linkDisabled = preview || unavailable;
  const detailsLink = { href: `/properties/${property.id}`, prefetch: linkDisabled ? false : undefined };

  return (
    <div className="relative grid">
      <article inert={linkDisabled} className="group flex flex-col overflow-hidden rounded-2xl border bg-surface shadow-sm transition-shadow hover:shadow-md">
        <Link {...detailsLink} className="relative block aspect-[4/3] overflow-hidden bg-surface-muted">
          {cover ? (
            <Image
              src={cover}
              alt={property.title}
              fill
              unoptimized
              preload={priority}
              sizes="(min-width: 1024px) 33vw, (min-width: 640px) 50vw, 100vw"
              className={cn("object-cover transition-transform duration-300 group-hover:scale-105", unavailable && "grayscale")}
            />
          ) : (
            <div className="flex h-full items-center justify-center text-muted-foreground">
              {!unavailable && <ImageOff className="size-8" aria-hidden />}
              <span className="sr-only">Sem foto</span>
            </div>
          )}
          <span className="absolute bottom-3 left-3 rounded-full bg-surface/90 px-2.5 py-0.5 text-xs font-medium text-foreground backdrop-blur">
            {propertyTypeLabels[property.type]} · {propertyPurposeLabels[property.purpose]}
          </span>
        </Link>
        <div className="flex flex-1 flex-col gap-2 p-4">
          <p className="text-xl font-semibold tracking-tight">
            {formatPrice(property.price)}
            {property.purpose === "Rent" && <span className="text-sm font-normal text-muted-foreground">/mês</span>}
          </p>
          <h3 className="line-clamp-2 font-medium leading-snug">
            <Link {...detailsLink} className="hover:text-accent">
              {property.title}
            </Link>
          </h3>
          <p className="flex items-center gap-1 text-sm text-muted-foreground">
            <MapPin className="size-4 shrink-0" aria-hidden />
            <span className="truncate">
              {property.neighborhood}, {property.city} - {property.state}
            </span>
          </p>
          <PropertySpecs
            className="mt-auto border-t pt-3"
            area={property.area}
            bedrooms={property.bedrooms}
            bathrooms={property.bathrooms}
            parkingSpaces={property.parkingSpaces}
          />
        </div>
      </article>
      {unavailable && (
        <p className="absolute inset-x-0 top-0 flex aspect-[4/3] items-center justify-center rounded-t-2xl bg-black/55 px-6 text-center text-sm font-medium text-white">
          <span className="sr-only">{property.title}: </span>
          Este imóvel não se encontra mais disponível.
        </p>
      )}
      {!preview && <SaveButton propertyId={property.id} className="absolute right-3 top-3" />}
    </div>
  );
}

/**
 * Vitrine em uma linha com rolagem lateral: a seção fica baixa mesmo com vários imóveis.
 * O `relative` mantém os textos `sr-only` dos cards dentro da rolagem; sem ele, eles alargam a página.
 */
export function PropertyRow({ properties, priority = false }: { properties: PropertySummary[]; priority?: boolean }) {
  return (
    <div className="relative -mx-4 flex snap-x scroll-px-4 gap-6 overflow-x-auto px-4 pb-3 [scrollbar-width:thin] sm:mx-0 sm:scroll-px-0 sm:px-0">
      {properties.map((property, index) => (
        <div key={property.id} className="grid w-72 shrink-0 snap-start sm:w-80">
          <PropertyCard property={property} priority={priority && index < 3} />
        </div>
      ))}
    </div>
  );
}
