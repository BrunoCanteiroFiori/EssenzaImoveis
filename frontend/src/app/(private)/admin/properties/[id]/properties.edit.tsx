"use client";

import { use, useCallback } from "react";
import Link from "next/link";
import { ArrowLeft, ExternalLink } from "lucide-react";
import { RequirePermission } from "@/components/admin/admin-shell";
import { useAuth } from "@/components/admin/auth-provider";
import { PageHeader, Panel } from "@/components/admin/page-header";
import { useApiData } from "@/components/admin/use-api-data";
import { Badge } from "@/components/ui/badge";
import { Alert, Spinner } from "@/components/ui/feedback";
import { propertyStatusLabels } from "@/lib/labels";
import { getProperty, updateProperty } from "@/lib/properties.api";
import type { Property, PropertyInput } from "@/lib/properties.types";
import { statusTones } from "../properties.component";
import { PropertyEditor } from "../properties.editor";
import { PropertyMediaManager } from "./properties.media";
import { PropertyStatusPanel } from "./properties.status";

export function EditPropertyPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  return (
    <RequirePermission permission="ReadProperties">
      <EditProperty id={id} />
    </RequirePermission>
  );
}

function EditProperty({ id }: { id: string }) {
  const { hasPermission, handleError } = useAuth();
  const load = useCallback(() => getProperty(id), [id]);
  const { data: property, error, loading, setData } = useApiData(load);
  const canWrite = hasPermission("WriteProperties");

  const handleUpdate = useCallback(
    async (input: PropertyInput) => {
      try {
        setData(await updateProperty(id, input));
      } catch (exception) {
        throw new Error(handleError(exception));
      }
    },
    [id, setData, handleError],
  );

  const handleChanged = useCallback((updated: Property) => setData(updated), [setData]);

  if (loading) return <Spinner />;
  if (error || !property) return <Alert>{error ?? "Imóvel não encontrado."}</Alert>;

  return (
    <>
      <Link href="/admin/properties" className="mb-4 inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
        <ArrowLeft className="size-4" aria-hidden /> Imóveis
      </Link>
      <PageHeader
        title={property.title}
        description={`${property.address.neighborhood}, ${property.address.city} - ${property.address.state}`}
        actions={
          <>
            <Badge tone={statusTones[property.status]}>{propertyStatusLabels[property.status]}</Badge>
            {property.status === "Available" && (
              <a
                href={`/properties/${property.id}`}
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center gap-1 text-sm font-medium text-accent hover:underline"
              >
                Ver no site <ExternalLink className="size-4" aria-hidden />
              </a>
            )}
          </>
        }
      />
      {!canWrite && <Alert className="mb-4">Você tem acesso somente de leitura a este imóvel.</Alert>}

      <PropertyEditor
        key={property.id}
        property={property}
        readOnly={!canWrite}
        submitLabel="Salvar alterações"
        onSubmit={handleUpdate}
        media={
          <Panel title="Fotos e vídeos">
            <PropertyMediaManager property={property} readOnly={!canWrite} onChanged={handleChanged} />
          </Panel>
        }
        aside={<PropertyStatusPanel property={property} readOnly={!canWrite} onChanged={handleChanged} />}
      />
    </>
  );
}
