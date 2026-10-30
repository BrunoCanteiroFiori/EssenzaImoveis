import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft } from "lucide-react";
import { PropertyDetailsView } from "@/components/properties/property-details-view";
import { SuggestedProperties } from "@/components/properties/suggested-properties";
import { ViewTracker } from "@/components/properties/view-tracker";
import { Skeleton } from "@/components/ui/feedback";
import { getPropertyDetails } from "@/lib/properties.api";

export async function PropertyDetailsComponent({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const property = await getPropertyDetails(id);
  if (!property) notFound();

  return (
    <div className="mx-auto flex max-w-7xl flex-col gap-6 px-4 py-8 sm:px-6">
      <ViewTracker propertyId={property.id} />
      <Link href="/properties" className="inline-flex w-fit items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
        <ArrowLeft className="size-4" aria-hidden /> Voltar para a busca
      </Link>

      <PropertyDetailsView property={property} />
      <SuggestedProperties key={property.id} propertyId={property.id} />
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
