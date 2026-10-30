"use client";

import { useEffect, useState, type ReactNode } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { ExternalLink, House, Inbox, KeyRound, LayoutDashboard, LogOut, ScrollText, Users } from "lucide-react";
import { Logo } from "@/components/layout/logo";
import { ThemeToggle } from "@/components/layout/theme-toggle";
import { Spinner } from "@/components/ui/feedback";
import type { Permission } from "@/lib/auth.api";
import { cn } from "@/lib/cn";
import { useAuth } from "./auth-provider";
import { ChangePasswordDialog } from "./change-password-dialog";

type NavItem = {
  href: string;
  label: string;
  icon: typeof House;
  permission?: Permission;
  superAdminOnly?: boolean;
};

const navItems: NavItem[] = [
  { href: "/admin/dashboard", label: "Painel", icon: LayoutDashboard },
  { href: "/admin/properties", label: "Imóveis", icon: House, permission: "ReadProperties" },
  { href: "/admin/contact-messages", label: "Mensagens", icon: Inbox, permission: "ManageMessages" },
  { href: "/admin/users", label: "Usuários", icon: Users, superAdminOnly: true },
  { href: "/admin/audit-logs", label: "Auditoria", icon: ScrollText, superAdminOnly: true },
];

/** Estrutura da área autenticada: verifica a sessão, mostra o menu conforme as permissões e redireciona ao login. */
export function AdminShell({ children }: { children: ReactNode }) {
  const { user, loading, isSuperAdmin, hasPermission, logout } = useAuth();
  const pathname = usePathname();
  const [changingPassword, setChangingPassword] = useState(false);

  // Navegação completa (não client-side): descarta da memória todo o estado da área administrativa ao sair.
  useEffect(() => {
    if (!loading && !user) {
      window.location.replace(`/auth/login?next=${encodeURIComponent(pathname)}`);
    }
  }, [loading, user, pathname]);

  if (loading || !user) {
    return <Spinner label="Verificando acesso..." />;
  }

  const visibleItems = navItems.filter(
    (item) => (!item.superAdminOnly || isSuperAdmin) && (!item.permission || hasPermission(item.permission)),
  );

  return (
    <div className="flex min-h-screen flex-col md:flex-row">
      <aside className="theme-brand border-b bg-surface md:sticky md:top-0 md:flex md:h-screen md:w-64 md:flex-col md:border-b-0 md:border-r">
        <div className="flex h-16 items-center justify-between px-4 md:px-5">
          <Logo href="/admin/dashboard" />
          <div className="md:hidden">
            <ThemeToggle />
          </div>
        </div>
        <nav aria-label="Área administrativa" className="scrollbar-none flex gap-1 overflow-x-auto px-3 pb-3 md:flex-1 md:flex-col md:overflow-visible md:pb-0">
          {visibleItems.map((item) => {
            const active = pathname.startsWith(item.href);
            return (
              <Link
                key={item.href}
                href={item.href}
                aria-current={active ? "page" : undefined}
                className={cn(
                  "flex shrink-0 items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors",
                  active ? "bg-accent-soft text-accent" : "text-muted-foreground hover:bg-surface-muted hover:text-foreground",
                )}
              >
                <item.icon className="size-4" aria-hidden />
                {item.label}
              </Link>
            );
          })}
          <Link
            href="/"
            target="_blank"
            className="flex shrink-0 items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium text-muted-foreground hover:bg-surface-muted hover:text-foreground md:mt-auto"
          >
            <ExternalLink className="size-4" aria-hidden />
            Ver site
          </Link>
        </nav>
        <div className="hidden border-t p-4 md:block">
          <p className="truncate text-sm font-medium">{user.name}</p>
          <p className="truncate text-xs text-muted-foreground">{isSuperAdmin ? "Super admin" : user.email}</p>
        </div>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex h-14 items-center justify-end gap-1 border-b bg-surface px-4">
          <div className="hidden md:block">
            <ThemeToggle />
          </div>
          <button
            type="button"
            onClick={() => setChangingPassword(true)}
            className="inline-flex items-center gap-2 rounded-lg px-3 py-2 text-sm text-muted-foreground hover:bg-surface-muted hover:text-foreground"
          >
            <KeyRound className="size-4" aria-hidden /> <span className="hidden sm:inline">Alterar senha</span>
          </button>
          <button
            type="button"
            onClick={() => void logout()}
            className="inline-flex items-center gap-2 rounded-lg px-3 py-2 text-sm text-muted-foreground hover:bg-surface-muted hover:text-foreground"
          >
            <LogOut className="size-4" aria-hidden /> Sair
          </button>
        </header>
        <main className="flex-1 p-4 sm:p-6 lg:p-8">{children}</main>
      </div>

      <ChangePasswordDialog open={changingPassword} onClose={() => setChangingPassword(false)} />
    </div>
  );
}

/** Mostra o conteúdo só se o usuário tiver a permissão; senão, um aviso. */
export function RequirePermission({
  permission,
  superAdminOnly,
  children,
}: {
  permission?: Permission;
  superAdminOnly?: boolean;
  children: ReactNode;
}) {
  const { isSuperAdmin, hasPermission } = useAuth();
  const allowed = (!superAdminOnly || isSuperAdmin) && (!permission || hasPermission(permission));
  if (allowed) return <>{children}</>;

  return (
    <div className="rounded-2xl border border-dashed bg-surface p-10 text-center text-muted-foreground">
      Você não tem permissão para acessar esta área. Fale com o administrador.
    </div>
  );
}
