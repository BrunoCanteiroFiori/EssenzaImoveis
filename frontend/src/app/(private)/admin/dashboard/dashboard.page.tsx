"use client";

import Link from "next/link";
import { Eye, House, Inbox, Star } from "lucide-react";
import { useAuth } from "@/components/admin/auth-provider";
import { PageHeader, Panel } from "@/components/admin/page-header";
import { useApiData } from "@/components/admin/use-api-data";
import { TemperatureBadge } from "@/components/properties/temperature-badge";
import { Alert, EmptyState, Spinner } from "@/components/ui/feedback";
import { getDashboardSummary } from "@/lib/dashboard.api";
import { formatPrice } from "@/lib/format";

export function DashboardPage() {
  const { user, hasPermission } = useAuth();
  const { data, error, loading } = useApiData(getDashboardSummary);

  if (loading) return <Spinner />;
  if (error || !data) return <Alert>{error}</Alert>;

  const cards = [
    { label: "Disponíveis", value: data.availableProperties, icon: House, href: "/admin/properties" },
    { label: "Em destaque", value: data.sponsoredProperties, icon: Star, href: "/admin/properties" },
    { label: "Mensagens novas", value: data.newMessages, icon: Inbox, href: "/admin/contact-messages" },
    { label: "Visitas (30 dias)", value: data.recentViews, icon: Eye, href: "/admin/properties" },
  ];

  const statuses = [
    { label: "Reservados", value: data.reservedProperties },
    { label: "Vendidos", value: data.soldProperties },
    { label: "Alugados", value: data.rentedProperties },
    { label: "Inativos", value: data.inactiveProperties },
  ];

  return (
    <>
      <PageHeader title={`Olá, ${user?.name.split(" ")[0] ?? ""}!`} description="Resumo dos imóveis e do atendimento." />
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {cards.map((card) => (
          <Link key={card.label} href={card.href} className="rounded-2xl border bg-surface p-5 shadow-sm transition-shadow hover:shadow-md">
            <div className="flex items-center justify-between text-muted-foreground">
              <span className="text-sm">{card.label}</span>
              <card.icon className="size-5" aria-hidden />
            </div>
            <p className="mt-2 text-3xl font-semibold tracking-tight">{card.value}</p>
          </Link>
        ))}
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-[1fr_20rem]">
        <Panel title="Imóveis mais quentes">
          {data.hottestProperties.length === 0 ? (
            <EmptyState title="Ainda não há visitas registradas." />
          ) : (
            <ul className="divide-y">
              {data.hottestProperties.map((property) => (
                <li key={property.id} className="flex flex-wrap items-center justify-between gap-3 py-3">
                  <div className="min-w-0">
                    {hasPermission("ReadProperties") ? (
                      <Link href={`/admin/properties/${property.id}`} className="font-medium hover:text-accent">
                        {property.title}
                      </Link>
                    ) : (
                      <p className="font-medium">{property.title}</p>
                    )}
                    <p className="text-sm text-muted-foreground">
                      {property.neighborhood}, {property.city} · {formatPrice(property.price)}
                    </p>
                  </div>
                  <div className="flex items-center gap-2 text-sm text-muted-foreground">
                    {property.temperature.recentViews} visitas
                    <TemperatureBadge temperature={property.temperature} />
                  </div>
                </li>
              ))}
            </ul>
          )}
        </Panel>
        <Panel title="Outros status">
          <dl className="divide-y">
            {statuses.map((status) => (
              <div key={status.label} className="flex justify-between py-2.5 text-sm">
                <dt className="text-muted-foreground">{status.label}</dt>
                <dd className="font-medium">{status.value}</dd>
              </div>
            ))}
          </dl>
        </Panel>
      </div>
    </>
  );
}
