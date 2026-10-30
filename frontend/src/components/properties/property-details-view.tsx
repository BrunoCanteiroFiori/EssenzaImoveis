import { Bath, BedDouble, Car, MapPin, MessageCircle, Receipt, Ruler, Wallet } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { buttonClass } from "@/components/ui/button";
import { formatArea, formatPrice } from "@/lib/format";
import { propertyPurposeLabels, propertyTypeLabels } from "@/lib/labels";
import type { PropertyDetails } from "@/lib/properties.types";
import { assetUrl, siteConfig } from "@/lib/site-config";
import { whatsappLink } from "@/lib/whatsapp";
import { ContactForm } from "./contact-form";
import { MediaCarousel } from "./media-carousel";

/**
 * Conteúdo da página pública do imóvel, compartilhado com a prévia da área administrativa.
 * O layout segue a largura do container (não a da tela) para funcionar também dentro da prévia.
 * Na prévia, os contatos ficam desativados.
 */
export function PropertyDetailsView({ property, preview = false }: { property: PropertyDetails; preview?: boolean }) {
  const media = property.media.map((item) => ({ type: item.type, url: assetUrl(item.url) ?? "" }));
  const pageUrl = `${siteConfig.url}/properties/${property.id}`;
  const whatsapp = whatsappLink(`Olá! Tenho interesse no imóvel "${property.title}" (${formatPrice(property.price)}). ${pageUrl}`);
  const specs = [
    { icon: Ruler, label: "Área", value: formatArea(property.area) },
    { icon: BedDouble, label: "Quartos", value: String(property.bedrooms) },
    { icon: Bath, label: "Banheiros", value: String(property.bathrooms) },
    { icon: Car, label: "Vagas", value: String(property.parkingSpaces) },
    { icon: Wallet, label: "Condomínio", value: property.condominiumFee ? formatPrice(property.condominiumFee) : "—" },
    { icon: Receipt, label: "IPTU (ano)", value: property.propertyTax ? formatPrice(property.propertyTax) : "—" },
  ];

  return (
    <div className="@container">
      <div className="grid gap-8 @4xl:grid-cols-[1fr_24rem]">
        <div className="flex min-w-0 flex-col gap-8">
          <MediaCarousel key={media.length} items={media} title={property.title} />

          <header className="flex flex-col gap-3">
            <div className="flex flex-wrap items-center gap-2">
              <Badge tone="accent">{propertyTypeLabels[property.type]}</Badge>
              <Badge>{propertyPurposeLabels[property.purpose]}</Badge>
            </div>
            <h1 className="text-3xl font-semibold tracking-tight text-balance">{property.title}</h1>
            <p className="flex items-center gap-1.5 text-muted-foreground">
              <MapPin className="size-4 shrink-0" aria-hidden />
              {property.neighborhood} — {property.city}/{property.state}
            </p>
          </header>

          <section className="flex flex-col gap-4">
            <h2 className="text-xl font-semibold">Características</h2>
            <dl className="grid grid-cols-2 gap-3 @xl:grid-cols-3">
              {specs.map((spec) => (
                <div key={spec.label} className="flex items-center gap-3 rounded-xl border bg-surface p-4">
                  <spec.icon className="size-5 text-accent" aria-hidden />
                  <div>
                    <dt className="text-xs text-muted-foreground">{spec.label}</dt>
                    <dd className="font-medium">{spec.value}</dd>
                  </div>
                </div>
              ))}
            </dl>
          </section>

          <section className="flex flex-col gap-3">
            <h2 className="text-xl font-semibold">Descrição</h2>
            <p className="whitespace-pre-line leading-relaxed text-muted-foreground">{property.description}</p>
          </section>
        </div>

        <aside inert={preview} className="flex flex-col gap-4 @4xl:sticky @4xl:top-20 @4xl:h-fit">
          <div className="flex flex-col gap-4 rounded-2xl border bg-surface p-6 shadow-sm">
            <div>
              <p className="text-sm text-muted-foreground">{property.purpose === "Rent" ? "Aluguel" : "Valor de venda"}</p>
              <p className="text-3xl font-semibold tracking-tight">
                {formatPrice(property.price)}
                {property.purpose === "Rent" && <span className="text-base font-normal text-muted-foreground">/mês</span>}
              </p>
            </div>
            {whatsapp && (
              <a href={whatsapp} target="_blank" rel="noopener noreferrer" className={buttonClass("primary", "lg", "w-full bg-[#25D366]! text-white! hover:bg-[#1ebe5b]!")}>
                <MessageCircle className="size-5" aria-hidden /> Falar no WhatsApp
              </a>
            )}
          </div>
          <div className="rounded-2xl border bg-surface p-6 shadow-sm">
            <h2 className="mb-4 font-semibold">Quero mais informações</h2>
            <ContactForm key={property.title} propertyId={property.id} propertyTitle={property.title} />
          </div>
        </aside>
      </div>
    </div>
  );
}
