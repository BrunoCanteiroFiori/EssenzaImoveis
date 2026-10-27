"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { ArrowLeft } from "lucide-react";
import { RequirePermission } from "@/components/admin/admin-shell";
import { useAuth } from "@/components/admin/auth-provider";
import { PageHeader, Panel } from "@/components/admin/page-header";
import { createProperty } from "@/lib/properties.api";
import type { PropertyInput } from "@/lib/properties.types";
import { PropertyForm } from "../properties.form";

export function NewPropertyPage() {
  const router = useRouter();
  const { handleError } = useAuth();

  async function handleSubmit(input: PropertyInput) {
    try {
      const property = await createProperty(input);
      router.push(`/admin/properties/${property.id}`);
    } catch (exception) {
      throw new Error(handleError(exception));
    }
  }

  return (
    <RequirePermission permission="WriteProperties">
      <Link href="/admin/properties" className="mb-4 inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
        <ArrowLeft className="size-4" aria-hidden /> Imóveis
      </Link>
      <PageHeader title="Novo imóvel" description="Depois de salvar, você poderá adicionar as fotos e destacar o anúncio." />
      <Panel>
        <PropertyForm submitLabel="Salvar e adicionar fotos" onSubmit={handleSubmit} />
      </Panel>
    </RequirePermission>
  );
}
