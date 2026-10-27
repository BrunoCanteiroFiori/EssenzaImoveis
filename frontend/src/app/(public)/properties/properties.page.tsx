import { Suspense } from "react";
import { Skeleton } from "@/components/ui/feedback";
import { PropertiesComponent, type SearchParams } from "./properties.component";

/** Página de busca: filtros + resultados, carregados no servidor a cada requisição. */
export function PropertiesPage({ searchParams }: { searchParams: Promise<SearchParams> }) {
  return (
    <div className="mx-auto flex max-w-7xl flex-col gap-6 px-4 py-10 sm:px-6">
      <div>
        <h1 className="text-3xl font-semibold tracking-tight">Imóveis</h1>
        <p className="text-muted-foreground">Use os filtros para encontrar o imóvel ideal.</p>
      </div>
      <Suspense fallback={<ResultsSkeleton />}>
        <PropertiesComponent searchParams={searchParams} />
      </Suspense>
    </div>
  );
}

function ResultsSkeleton() {
  return (
    <div className="grid gap-8 lg:grid-cols-[18rem_1fr]">
      <Skeleton className="h-[32rem]" />
      <div className="grid gap-6 sm:grid-cols-2 xl:grid-cols-3">
        {Array.from({ length: 6 }, (_, index) => (
          <Skeleton key={index} className="aspect-[4/5]" />
        ))}
      </div>
    </div>
  );
}
