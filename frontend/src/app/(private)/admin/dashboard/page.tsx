import type { Metadata } from "next";
import { DashboardPage } from "./dashboard.page";

export const instant = false;

export const metadata: Metadata = { title: "Painel" };

export default function Page() {
  return <DashboardPage />;
}
