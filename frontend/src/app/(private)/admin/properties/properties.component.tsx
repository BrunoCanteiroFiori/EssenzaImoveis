"use client";

import { useState } from "react";
import Image from "next/image";
import Link from "next/link";
import { ExternalLink, ImageOff, Pencil, Star, Trash2 } from "lucide-react";
import { useAuth } from "@/components/admin/auth-provider";
import { TemperatureBadge } from "@/components/properties/temperature-badge";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Dialog } from "@/components/ui/dialog";
import { Alert, EmptyState } from "@/components/ui/feedback";
import { formatPrice } from "@/lib/format";
import { propertyPurposeLabels, propertyStatusLabels, propertyTypeLabels } from "@/lib/labels";
import { deleteProperty } from "@/lib/properties.api";
import type { PropertyStatus, PropertySummary } from "@/lib/properties.types";
import { assetUrl } from "@/lib/site-config";

export const statusTones: Record<PropertyStatus, "success" | "warning" | "info" | "neutral" | "accent"> = {
  Available: "success",
  Reserved: "warning",
  Sold: "info",
  Rented: "accent",
  Inactive: "neutral",
};

/** Tabela de imóveis da gestão, com ações conforme as permissões do usuário. */
export function PropertiesComponent({ properties, onChanged }: { properties: PropertySummary[]; onChanged: () => void }) {
  const { hasPermission, handleError } = useAuth();
  const [deleting, setDeleting] = useState<PropertySummary | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function confirmDelete() {
    if (!deleting) return;
    setBusy(true);
    try {
      await deleteProperty(deleting.id);
      setDeleting(null);
      onChanged();
    } catch (exception) {
      setError(handleError(exception));
    } finally {
      setBusy(false);
    }
  }

  if (properties.length === 0) {
    return <EmptyState title="Nenhum imóvel encontrado." />;
  }

  return (
    <>
      <div className="overflow-x-auto rounded-2xl border bg-surface">
        <table className="w-full min-w-[56rem] text-sm">
          <thead className="border-b bg-surface-muted text-left text-xs uppercase tracking-wide text-muted-foreground">
            <tr>
              <th className="px-4 py-3 font-medium">Imóvel</th>
              <th className="px-4 py-3 font-medium">Tipo</th>
              <th className="px-4 py-3 font-medium">Valor</th>
              <th className="px-4 py-3 font-medium">Status</th>
              <th className="px-4 py-3 font-medium">Temperatura</th>
              <th className="px-4 py-3 text-right font-medium">Ações</th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {properties.map((property) => {
              const cover = assetUrl(property.coverImageUrl);
              return (
                <tr key={property.id} className="hover:bg-surface-muted/50">
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-3">
                      <div className="relative size-14 shrink-0 overflow-hidden rounded-lg bg-surface-muted">
                        {cover ? (
                          <Image src={cover} alt="" fill unoptimized sizes="56px" className="object-cover" />
                        ) : (
                          <ImageOff className="m-auto mt-4 size-5 text-muted-foreground" aria-hidden />
                        )}
                      </div>
                      <div className="min-w-0">
                        <p className="flex items-center gap-1.5 font-medium">
                          {property.isSponsored && <Star className="size-4 shrink-0 fill-amber-400 text-amber-400" aria-label="Em destaque" />}
                          <span className="truncate">{property.title}</span>
                        </p>
                        <p className="truncate text-xs text-muted-foreground">
                          {property.neighborhood}, {property.city} - {property.state} · {property.imageCount} foto(s)
                        </p>
                      </div>
                    </div>
                  </td>
                  <td className="px-4 py-3 text-muted-foreground">
                    {propertyTypeLabels[property.type]}
                    <br />
                    {propertyPurposeLabels[property.purpose]}
                  </td>
                  <td className="px-4 py-3 font-medium">{formatPrice(property.price)}</td>
                  <td className="px-4 py-3">
                    <Badge tone={statusTones[property.status]}>{propertyStatusLabels[property.status]}</Badge>
                  </td>
                  <td className="px-4 py-3">
                    <TemperatureBadge temperature={property.temperature} />
                    <p className="mt-1 text-xs text-muted-foreground">{property.temperature.recentViews} visitas</p>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex justify-end gap-1">
                      {property.status === "Available" && (
                        <a
                          href={`/properties/${property.id}`}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="rounded-lg p-2 text-muted-foreground hover:bg-surface-muted hover:text-foreground"
                          title="Ver no site"
                        >
                          <ExternalLink className="size-4" aria-hidden />
                          <span className="sr-only">Ver no site</span>
                        </a>
                      )}
                      <Link
                        href={`/admin/properties/${property.id}`}
                        className="rounded-lg p-2 text-muted-foreground hover:bg-surface-muted hover:text-foreground"
                        title={hasPermission("WriteProperties") ? "Editar" : "Ver detalhes"}
                      >
                        <Pencil className="size-4" aria-hidden />
                        <span className="sr-only">{hasPermission("WriteProperties") ? "Editar" : "Ver detalhes"}</span>
                      </Link>
                      {hasPermission("DeleteProperties") && (
                        <button
                          type="button"
                          onClick={() => {
                            setError(null);
                            setDeleting(property);
                          }}
                          className="rounded-lg p-2 text-danger hover:bg-danger-soft"
                          title="Excluir"
                        >
                          <Trash2 className="size-4" aria-hidden />
                          <span className="sr-only">Excluir</span>
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      <Dialog open={deleting !== null} title="Excluir imóvel" onClose={() => setDeleting(null)}>
        <p className="text-sm text-muted-foreground">
          Excluir <strong className="text-foreground">{deleting?.title}</strong> e todas as fotos? Esta ação não pode ser desfeita (fica
          registrada na auditoria).
        </p>
        {error && <Alert>{error}</Alert>}
        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={() => setDeleting(null)}>
            Cancelar
          </Button>
          <Button variant="danger" loading={busy} onClick={() => void confirmDelete()}>
            <Trash2 className="size-4" aria-hidden /> Excluir
          </Button>
        </div>
      </Dialog>
    </>
  );
}
