import { Suspense, type ReactNode } from "react";
import Link from "next/link";
import { ArrowRight, Handshake, House, KeyRound, MessageCircle, ShieldCheck, Sparkles, Star, TrendingUp } from "lucide-react";
import { ContactForm } from "@/components/properties/contact-form";
import { PropertyRow } from "@/components/properties/property-card";
import { SearchForm } from "@/components/properties/search-form";
import { buttonClass } from "@/components/ui/button";
import { EmptyState, Skeleton } from "@/components/ui/feedback";
import { getFeaturedProperties } from "@/lib/properties.api";
import type { PropertySummary } from "@/lib/properties.types";
import { whatsappLink } from "@/lib/whatsapp";

export default function HomePage() {
  const whatsapp = whatsappLink("Olá! Vim pelo site e gostaria de ajuda para encontrar um imóvel.");

  return (
    <>
      <section className="relative overflow-hidden bg-background bg-gradient-to-b from-accent-soft to-background dark:from-accent-soft/60">
        <div className="mx-auto flex max-w-7xl flex-col gap-8 px-4 pb-14 pt-12 sm:px-6 md:pt-20">
          <div className="flex max-w-2xl flex-col gap-4">
            <span className="inline-flex w-fit items-center gap-1.5 rounded-full bg-surface px-3 py-1 text-xs font-medium text-accent shadow-sm">
              <Sparkles className="size-3.5" aria-hidden /> Imóveis selecionados para você
            </span>
            <h1 className="text-4xl font-semibold tracking-tight text-balance sm:text-5xl">Encontre o imóvel certo para a sua próxima fase.</h1>
            <p className="text-lg text-muted-foreground text-pretty">
              Busque por cidade, bairro ou tipo de imóvel e filtre por quartos, vagas, metragem e valor.
            </p>
          </div>
          <SearchForm />
        </div>
      </section>

      <Suspense fallback={<FeaturedSkeleton />}>
        <FeaturedSections />
      </Suspense>

      <section className="border-y bg-surface">
        <div className="mx-auto grid max-w-7xl gap-6 px-4 py-14 sm:px-6 md:grid-cols-3">
          {[
            { icon: ShieldCheck, title: "Imóveis verificados", text: "Documentação e informações conferidas pela nossa equipe antes do anúncio." },
            { icon: Handshake, title: "Atendimento próximo", text: "Um corretor acompanha você da primeira visita até a assinatura." },
            { icon: KeyRound, title: "Compra e aluguel", text: "Opções para morar, investir ou instalar o seu negócio." },
          ].map((item) => (
            <div key={item.title} className="flex gap-4">
              <span className="flex size-11 shrink-0 items-center justify-center rounded-xl bg-accent-soft text-accent">
                <item.icon className="size-5" aria-hidden />
              </span>
              <div>
                <h2 className="font-semibold">{item.title}</h2>
                <p className="text-sm text-muted-foreground">{item.text}</p>
              </div>
            </div>
          ))}
        </div>
      </section>

      <section id="contato" className="mx-auto grid max-w-7xl scroll-mt-20 gap-10 px-4 py-16 sm:px-6 lg:grid-cols-2">
        <div className="flex flex-col gap-4">
          <h2 className="text-3xl font-semibold tracking-tight">Fale com a gente</h2>
          <p className="text-muted-foreground">
            Não encontrou o que procura? Conte o que você precisa e um corretor retorna o seu contato. Se preferir, chame no WhatsApp.
          </p>
          {whatsapp && (
            <a href={whatsapp} target="_blank" rel="noopener noreferrer" className={buttonClass("secondary", "lg", "w-fit")}>
              <MessageCircle className="size-5 text-[#25D366]" aria-hidden /> Conversar no WhatsApp
            </a>
          )}
        </div>
        <div className="rounded-2xl border bg-surface p-6 shadow-sm">
          <ContactForm />
        </div>
      </section>
    </>
  );
}

async function FeaturedSections() {
  const featured = await getFeaturedProperties();
  if (!featured) {
    return (
      <div className="mx-auto max-w-7xl px-4 py-14 sm:px-6">
        <EmptyState title="Não foi possível carregar os imóveis agora.">Tente novamente em alguns instantes.</EmptyState>
      </div>
    );
  }

  if (featured.newest.length === 0) {
    return (
      <div className="mx-auto max-w-7xl px-4 py-14 sm:px-6">
        <EmptyState title="Em breve novos imóveis por aqui.">Enquanto isso, deixe uma mensagem que avisamos você.</EmptyState>
      </div>
    );
  }

  return (
    <div className="mx-auto flex max-w-7xl flex-col gap-14 px-4 py-14 sm:px-6">
      <FeaturedSection
        title="Destaques"
        description="Imóveis em evidência selecionados pela corretora."
        icon={<Star className="size-5 fill-gold text-gold" aria-hidden />}
        properties={featured.highlights}
        href="/properties"
        priority
      />
      <FeaturedSection
        title="Compra"
        description="As melhores oportunidades para comprar."
        icon={<House className="size-5 text-accent" aria-hidden />}
        properties={featured.forSale}
        href="/properties?purpose=Sale"
      />
      <FeaturedSection
        title="Aluguel"
        description="As melhores opções para alugar."
        icon={<KeyRound className="size-5 text-accent" aria-hidden />}
        properties={featured.forRent}
        href="/properties?purpose=Rent"
      />
      <FeaturedSection
        title="Mais buscados"
        description="Os imóveis que mais chamam a atenção por aqui."
        icon={<TrendingUp className="size-5 text-accent" aria-hidden />}
        properties={featured.mostSearched}
      />
      <FeaturedSection
        title="Recém-chegados"
        description="Os anúncios mais novos do site."
        icon={<Sparkles className="size-5 text-accent" aria-hidden />}
        properties={featured.newest}
        href="/properties?sort=Newest"
      />
    </div>
  );
}

type FeaturedSectionProps = {
  title: string;
  description: string;
  icon: ReactNode;
  properties: PropertySummary[];
  href?: string;
  priority?: boolean;
};

function FeaturedSection({ title, description, icon, properties, href, priority }: FeaturedSectionProps) {
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

function FeaturedSkeleton() {
  return (
    <div className="mx-auto grid max-w-7xl gap-6 px-4 py-14 sm:grid-cols-2 sm:px-6 lg:grid-cols-3">
      {Array.from({ length: 3 }, (_, index) => (
        <Skeleton key={index} className="aspect-[4/5]" />
      ))}
    </div>
  );
}
