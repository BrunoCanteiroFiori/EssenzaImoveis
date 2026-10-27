import type { Metadata } from "next";
import { NewPropertyPage } from "./properties.new";

export const instant = false;

export const metadata: Metadata = { title: "Novo imóvel" };

export default function Page() {
  return <NewPropertyPage />;
}
