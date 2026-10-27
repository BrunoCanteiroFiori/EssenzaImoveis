import { Suspense } from "react";
import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getPropertyDetails } from "@/lib/properties.api";
import { formatPrice } from "@/lib/format";
import { propertyPurposeLabels, propertyTypeLabels } from "@/lib/labels";
import { assetUrl } from "@/lib/site-config";
import { DetailsSkeleton, PropertyDetailsComponent } from "./properties.details";

export async function generateMetadata({ params }: PageProps<"/properties/[id]">): Promise<Metadata> {
  const { id } = await params;
  const property = await getPropertyDetails(id);
  if (!property) notFound();

  const cover = assetUrl(property.imageUrls[0]);
  const title = `${property.title} - ${property.neighborhood}, ${property.city}`;
  const description = `${propertyTypeLabels[property.type]} para ${propertyPurposeLabels[property.purpose].toLowerCase()} por ${formatPrice(property.price)}. ${property.description.slice(0, 120)}`;

  return {
    title,
    description,
    openGraph: { title, description, images: cover ? [cover] : undefined },
  };
}

export default function Page({ params }: PageProps<"/properties/[id]">) {
  return (
    <Suspense fallback={<DetailsSkeleton />}>
      <PropertyDetailsComponent params={params} />
    </Suspense>
  );
}
