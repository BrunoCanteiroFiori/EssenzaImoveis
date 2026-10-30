import Image from "next/image";
import Link from "next/link";
import { ImageOff, MapPin } from "lucide-react";
import { formatPrice } from "@/lib/format";
import { propertyPurposeLabels, propertyTypeLabels } from "@/lib/labels";
import type { PropertySummary } from "@/lib/properties.types";
import { assetUrl } from "@/lib/site-config";
import { cn } from "@/lib/cn";
import { PropertySpecs } from "./property-specs";
import { SponsoredBadge } from "./sponsored-badge";
import { TemperatureBadge } from "./temperature-badge";

type PropertyCardProps = {
  property: PropertySummary;
  priority?: boolean;
  /** Prévia da área administrativa: mesmo visual, com os links desativados. */
  preview?: boolean;
};

export function PropertyCard({ property, priority = false, preview = false }: PropertyCardProps) {
  const cover = assetUrl(property.coverImageUrl);
  const detailsLink = { href: `/properties/${property.id}`, prefetch: preview ? false : undefined };

  return (
    <article
      inert={preview}
      className={cn(
        "group flex flex-col overflow-hidden rounded-2xl border bg-surface shadow-sm transition-shadow hover:shadow-md",
        property.isSponsored && "ring-2 ring-gold/70",
      )}
    >
      <Link {...detailsLink} className="relative block aspect-[4/3] overflow-hidden bg-surface-muted">
        {cover ? (
          <Image
            src={cover}
            alt={property.title}
            fill
            unoptimized
            preload={priority}
            sizes="(min-width: 1024px) 33vw, (min-width: 640px) 50vw, 100vw"
            className="object-cover transition-transform duration-300 group-hover:scale-105"
          />
        ) : (
          <div className="flex h-full items-center justify-center text-muted-foreground">
            <ImageOff className="size-8" aria-hidden />
            <span className="sr-only">Sem foto</span>
          </div>
        )}
        <div className="absolute left-3 top-3 flex flex-wrap gap-1.5">
          {property.isSponsored && <SponsoredBadge />}
          <TemperatureBadge temperature={property.temperature} />
        </div>
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
  );
}

export function PropertyGrid({ properties }: { properties: PropertySummary[] }) {
  return (
    <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
      {properties.map((property, index) => (
        <PropertyCard key={property.id} property={property} priority={index < 3} />
      ))}
    </div>
  );
}
