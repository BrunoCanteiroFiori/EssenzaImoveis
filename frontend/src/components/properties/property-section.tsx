import type { ReactNode } from "react";
import Link from "next/link";
import { ArrowRight } from "lucide-react";
import type { PropertySummary } from "@/lib/properties.types";
import { PropertyRow } from "./property-card";

type PropertySectionProps = {
  title: string;
  description: string;
  icon: ReactNode;
  properties: PropertySummary[];
  href?: string;
  priority?: boolean;
};

/** Seção de vitrine: título e a linha de cards. Sem imóveis, não aparece. */
export function PropertySection({ title, description, icon, properties, href, priority }: PropertySectionProps) {
  if (properties.length === 0) return null;

  return (
    <section className="flex flex-col gap-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h2 className="flex items-center gap-2 text-2xl font-semibold tracking-tight">
            {icon}
            {title}
          </h2>
          <p className="text-muted-foreground">{description}</p>
        </div>
        {href && (
          <Link href={href} className="inline-flex items-center gap-1 text-sm font-medium text-accent hover:underline">
            Ver todos <ArrowRight className="size-4" aria-hidden />
          </Link>
        )}
      </div>
      <PropertyRow properties={properties} priority={priority} />
    </section>
  );
}
