import type { Metadata } from "next";
import { PropertiesPage } from "./properties.page";

export const metadata: Metadata = {
  title: "Imóveis à venda e para alugar",
  description: "Busque casas, apartamentos, terrenos e imóveis comerciais por localidade, tipo, quartos, vagas, metragem e valor.",
};

export default function Page({ searchParams }: PageProps<"/properties">) {
  return <PropertiesPage searchParams={searchParams} />;
}
