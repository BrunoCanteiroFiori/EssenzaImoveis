import type { Metadata } from "next";
import { PropertiesPage } from "./properties.page";

export const instant = false;

export const metadata: Metadata = { title: "Imóveis" };

export default function Page() {
  return <PropertiesPage />;
}
