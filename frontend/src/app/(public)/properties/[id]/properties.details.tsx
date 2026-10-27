import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft, Bath, BedDouble, Car, MapPin, MessageCircle, Receipt, Ruler, Wallet } from "lucide-react";
import { ContactForm } from "@/components/properties/contact-form";
import { ImageCarousel } from "@/components/properties/image-carousel";
import { SponsoredBadge } from "@/components/properties/sponsored-badge";
import { TemperatureBadge } from "@/components/properties/temperature-badge";
import { ViewTracker } from "@/components/properties/view-tracker";
import { buttonClass } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/feedback";
import { formatArea, formatPrice } from "@/lib/format";
import { propertyPurposeLabels, propertyTypeLabels } from "@/lib/labels";
import { getPropertyDetails } from "@/lib/properties.api";
import { assetUrl, siteConfig } from "@/lib/site-config";
import { whatsappLink } from "@/lib/whatsapp";

export async function PropertyDetailsComponent({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const property = await getPropertyDetails(id);
  if (!property) notFound();

  const images = property.imageUrls.map((url) => assetUrl(url)).filter((url): url is string => url !== null);
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
    <div className="mx-auto flex max-w-7xl flex-col gap-6 px-4 py-8 sm:px-6">
      <ViewTracker propertyId={property.id} />
      <Link href="/properties" className="inline-flex w-fit items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
        <ArrowLeft className="size-4" aria-hidden /> Voltar para a busca
      </Link>

      <div className="grid gap-8 lg:grid-cols-[1fr_24rem]">
        <div className="flex min-w-0 flex-col gap-8">
          <ImageCarousel images={images} title={property.title} />

          <header className="flex flex-col gap-3">
            <div className="flex flex-wrap items-center gap-2">
              {property.isSponsored && <SponsoredBadge />}
              <TemperatureBadge temperature={property.temperature} />
              <Badge tone="accent">{propertyTypeLabels[property.type]}</Badge>
              <Badge>{propertyPurposeLabels[property.purpose]}</Badge>
            </div>
            <h1 className="text-3xl font-semibold tracking-tight text-balance">{property.title}</h1>
            <p className="flex items-center gap-1.5 text-muted-foreground">
              <MapPin className="size-4 shrink-0" aria-hidden />
              {property.street}, {property.neighborhood} — {property.city}/{property.state}
            </p>
          </header>

          <section aria-labelledby="specs-title" className="flex flex-col gap-4">
            <h2 id="specs-title" className="text-xl font-semibold">
              Características
            </h2>
            <dl className="grid grid-cols-2 gap-3 sm:grid-cols-3">
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

          <section aria-labelledby="description-title" className="flex flex-col gap-3">
            <h2 id="description-title" className="text-xl font-semibold">
              Descrição
            </h2>
            <p className="whitespace-pre-line leading-relaxed text-muted-foreground">{property.description}</p>
          </section>
        </div>

        <aside className="flex flex-col gap-4 lg:sticky lg:top-20 lg:h-fit">
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
            <ContactForm propertyId={property.id} propertyTitle={property.title} />
          </div>
        </aside>
      </div>
    </div>
  );
}

export function DetailsSkeleton() {
  return (
    <div className="mx-auto grid max-w-7xl gap-8 px-4 py-14 sm:px-6 lg:grid-cols-[1fr_24rem]">
      <div className="flex flex-col gap-6">
        <Skeleton className="aspect-[16/10]" />
        <Skeleton className="h-10 w-2/3" />
        <Skeleton className="h-32" />
      </div>
      <Skeleton className="h-96" />
    </div>
  );
}
