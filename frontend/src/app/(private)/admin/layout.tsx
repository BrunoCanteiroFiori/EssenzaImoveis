import { Suspense } from "react";
import type { Metadata } from "next";
import { AdminShell } from "@/components/admin/admin-shell";
import { AuthProvider } from "@/components/admin/auth-provider";
import { Spinner } from "@/components/ui/feedback";

/**
 * A área administrativa só renderiza depois de confirmar a sessão no navegador (os cookies httpOnly pertencem à API),
 * então não existe UI instantânea a validar na navegação para estas rotas.
 */
export const instant = false;

export const metadata: Metadata = {
  title: { default: "Área administrativa", template: "%s | Área administrativa" },
  robots: { index: false, follow: false },
};

export default function AdminLayout({ children }: LayoutProps<"/admin">) {
  return (
    <AuthProvider>
      <Suspense fallback={<Spinner />}>
        <AdminShell>{children}</AdminShell>
      </Suspense>
    </AuthProvider>
  );
}
