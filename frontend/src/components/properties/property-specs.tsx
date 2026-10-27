import { Bath, BedDouble, Car, Ruler } from "lucide-react";
import { cn } from "@/lib/cn";
import { formatArea } from "@/lib/format";

type PropertySpecsProps = {
  area: number;
  bedrooms: number;
  bathrooms: number;
  parkingSpaces: number;
  className?: string;
};

/** Linha compacta com metragem, quartos, banheiros e vagas (itens zerados não aparecem). */
export function PropertySpecs({ area, bedrooms, bathrooms, parkingSpaces, className }: PropertySpecsProps) {
  const items = [
    { icon: Ruler, label: formatArea(area), title: "Área", show: true },
    { icon: BedDouble, label: String(bedrooms), title: "Quartos", show: bedrooms > 0 },
    { icon: Bath, label: String(bathrooms), title: "Banheiros", show: bathrooms > 0 },
    { icon: Car, label: String(parkingSpaces), title: "Vagas de garagem", show: parkingSpaces > 0 },
  ];

  return (
    <ul className={cn("flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-muted-foreground", className)}>
      {items
        .filter((item) => item.show)
        .map((item) => (
          <li key={item.title} className="inline-flex items-center gap-1.5" title={item.title}>
            <item.icon className="size-4" aria-hidden />
            <span>{item.label}</span>
            <span className="sr-only">{item.title}</span>
          </li>
        ))}
    </ul>
  );
}
