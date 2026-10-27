import type { Metadata } from "next";
import { UsersPage } from "./users.page";

export const instant = false;

export const metadata: Metadata = { title: "Usuários" };

export default function Page() {
  return <UsersPage />;
}
