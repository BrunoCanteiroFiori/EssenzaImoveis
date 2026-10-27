import { Suspense } from "react";
import type { Metadata } from "next";
import { Spinner } from "@/components/ui/feedback";
import { EditPropertyPage } from "./properties.edit";

export const instant = false;

export const metadata: Metadata = { title: "Editar imóvel" };

export default function Page({ params }: PageProps<"/admin/properties/[id]">) {
  return (
    <Suspense fallback={<Spinner />}>
      <EditPropertyPage params={params} />
    </Suspense>
  );
}
