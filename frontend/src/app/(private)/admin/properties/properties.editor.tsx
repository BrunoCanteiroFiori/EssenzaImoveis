"use client";

import { useState, type ReactNode } from "react";
import { Panel } from "@/components/admin/page-header";
import type { Property, PropertyInput } from "@/lib/properties.types";
import { toPropertyDraft } from "./properties.draft";
import { PropertyForm } from "./properties.form";
import { PropertyPreview } from "./properties.preview";

type PropertyEditorProps = {
  property?: Property;
  readOnly?: boolean;
  submitLabel: string;
  onSubmit: (input: PropertyInput) => Promise<void>;
  media?: ReactNode;
  aside?: ReactNode;
};

/** Formulário do imóvel com a prévia ao lado, ligados pelo mesmo rascunho. */
export function PropertyEditor({ property, readOnly, submitLabel, onSubmit, media, aside }: PropertyEditorProps) {
  const [draft, setDraft] = useState(() => toPropertyDraft(property));

  return (
    <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_26rem]">
      <div className="flex min-w-0 flex-col gap-6">
        {media}
        <Panel title="Dados do imóvel">
          <PropertyForm draft={draft} onDraftChange={setDraft} readOnly={readOnly} submitLabel={submitLabel} onSubmit={onSubmit} />
        </Panel>
      </div>
      <div className="flex min-w-0 flex-col gap-6">
        {aside}
        <PropertyPreview draft={draft} property={property} />
      </div>
    </div>
  );
}
