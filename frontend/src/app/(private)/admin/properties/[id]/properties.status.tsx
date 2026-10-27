"use client";

import { useId, useState, type FormEvent } from "react";
import { Megaphone, Star } from "lucide-react";
import { useAuth } from "@/components/admin/auth-provider";
import { Panel } from "@/components/admin/page-header";
import { Button } from "@/components/ui/button";
import { Field, Input, Select } from "@/components/ui/field";
import { Alert } from "@/components/ui/feedback";
import { formatDateTime } from "@/lib/format";
import { propertyStatusLabels, propertyStatuses } from "@/lib/labels";
import { changePropertyStatus, removePropertySponsorship, sponsorProperty } from "@/lib/properties.api";
import type { Property, PropertyStatus } from "@/lib/properties.types";

/** Status que fazem sentido para a finalidade (vendido só para venda, alugado só para aluguel). */
function allowedStatuses(property: Property) {
  return propertyStatuses.filter(
    (status) => !(status === "Sold" && property.purpose !== "Sale") && !(status === "Rented" && property.purpose !== "Rent"),
  );
}

type StatusPanelProps = { property: Property; readOnly: boolean; onChanged: (property: Property) => void };

export function PropertyStatusPanel({ property, readOnly, onChanged }: StatusPanelProps) {
  const { handleError } = useAuth();
  const idPrefix = useId();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function run(action: () => Promise<Property>) {
    setBusy(true);
    setError(null);
    try {
      onChanged(await action());
    } catch (exception) {
      setError(handleError(exception));
    } finally {
      setBusy(false);
    }
  }

  function handleStatus(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const status = String(new FormData(event.currentTarget).get("status")) as PropertyStatus;
    void run(() => changePropertyStatus(property.id, status));
  }

  function handleSponsor(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const days = Number(new FormData(event.currentTarget).get("days"));
    void run(() => sponsorProperty(property.id, days));
  }

  return (
    <div className="flex flex-col gap-6">
      <Panel title="Status do anúncio">
        <form onSubmit={handleStatus} className="flex flex-col gap-3">
          <fieldset disabled={readOnly || busy} className="flex flex-col gap-3">
            <Field label="Status" htmlFor={`${idPrefix}-status`} hint="Só imóveis disponíveis aparecem no site.">
              <Select id={`${idPrefix}-status`} name="status" key={property.status} defaultValue={property.status}>
                {allowedStatuses(property).map((status) => (
                  <option key={status} value={status}>
                    {propertyStatusLabels[status]}
                  </option>
                ))}
              </Select>
            </Field>
            {!readOnly && (
              <Button type="submit" variant="secondary" loading={busy}>
                Atualizar status
              </Button>
            )}
          </fieldset>
        </form>
      </Panel>

      <Panel title="Destaque (patrocínio)">
        {property.isSponsored && property.sponsoredUntil ? (
          <div className="flex flex-col gap-3">
            <p className="flex items-center gap-2 text-sm">
              <Star className="size-4 fill-amber-400 text-amber-400" aria-hidden />
              Em destaque até <strong>{formatDateTime(property.sponsoredUntil)}</strong>
            </p>
            {!readOnly && (
              <Button variant="secondary" loading={busy} onClick={() => void run(() => removePropertySponsorship(property.id))}>
                Encerrar destaque
              </Button>
            )}
          </div>
        ) : (
          <form onSubmit={handleSponsor} className="flex flex-col gap-3">
            <p className="text-sm text-muted-foreground">
              Imóveis em destaque aparecem primeiro na busca, na página inicial e com selo especial.
            </p>
            <fieldset disabled={readOnly || busy || property.status !== "Available"} className="flex flex-col gap-3">
              <Field label="Duração (dias)" htmlFor={`${idPrefix}-days`}>
                <Input id={`${idPrefix}-days`} name="days" type="number" min={1} max={365} defaultValue={30} required />
              </Field>
              {!readOnly && (
                <Button type="submit" loading={busy}>
                  <Megaphone className="size-4" aria-hidden /> Destacar imóvel
                </Button>
              )}
            </fieldset>
            {property.status !== "Available" && <p className="text-xs text-muted-foreground">Disponível apenas para imóveis com status Disponível.</p>}
          </form>
        )}
      </Panel>

      {error && <Alert>{error}</Alert>}
    </div>
  );
}
