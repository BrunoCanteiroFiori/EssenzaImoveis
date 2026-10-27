"use client";

import { useState, type ReactNode } from "react";
import { KeyRound, Pencil, ShieldCheck, UserCheck, UserPlus, UserX } from "lucide-react";
import { RequirePermission } from "@/components/admin/admin-shell";
import { useAuth } from "@/components/admin/auth-provider";
import { PageHeader } from "@/components/admin/page-header";
import { useApiData } from "@/components/admin/use-api-data";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Alert, Spinner } from "@/components/ui/feedback";
import { formatDateTime } from "@/lib/format";
import { activateUser, deactivateUser, listUsers, permissionLabels, type User } from "@/lib/users.api";
import { ResetPasswordDialog, UserFormDialog } from "./users.form";

type DialogState = { kind: "create" } | { kind: "edit"; user: User } | { kind: "password"; user: User } | null;

export function UsersPage() {
  return (
    <RequirePermission superAdminOnly>
      <UsersList />
    </RequirePermission>
  );
}

function UsersList() {
  const { handleError } = useAuth();
  const { data: users, error, loading, reload } = useApiData(listUsers);
  const [dialog, setDialog] = useState<DialogState>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  async function toggleActive(user: User) {
    setActionError(null);
    try {
      await (user.isActive ? deactivateUser(user.id) : activateUser(user.id));
      reload();
    } catch (exception) {
      setActionError(handleError(exception));
    }
  }

  function closeDialog(changed: boolean) {
    setDialog(null);
    if (changed) reload();
  }

  return (
    <>
      <PageHeader
        title="Usuários e permissões"
        description="Somente o super admin cria contas e define o que cada pessoa pode fazer."
        actions={
          <Button onClick={() => setDialog({ kind: "create" })}>
            <UserPlus className="size-4" aria-hidden /> Novo usuário
          </Button>
        }
      />

      {(error || actionError) && <Alert className="mb-4">{error ?? actionError}</Alert>}
      {loading ? (
        <Spinner />
      ) : (
        <div className="overflow-x-auto rounded-2xl border bg-surface">
          <table className="w-full min-w-[52rem] text-sm">
            <thead className="border-b bg-surface-muted text-left text-xs uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-4 py-3 font-medium">Usuário</th>
                <th className="px-4 py-3 font-medium">Permissões</th>
                <th className="px-4 py-3 font-medium">Situação</th>
                <th className="px-4 py-3 font-medium">Último acesso</th>
                <th className="px-4 py-3 text-right font-medium">Ações</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {users?.map((user) => {
                const isSuperAdmin = user.role === "SuperAdmin";
                return (
                  <tr key={user.id} className={user.isActive ? "" : "opacity-60"}>
                    <td className="px-4 py-3">
                      <p className="flex items-center gap-1.5 font-medium">
                        {isSuperAdmin && <ShieldCheck className="size-4 text-accent" aria-label="Super admin" />}
                        {user.name}
                      </p>
                      <p className="text-xs text-muted-foreground">{user.email}</p>
                    </td>
                    <td className="px-4 py-3">
                      {isSuperAdmin ? (
                        <Badge tone="accent">Acesso total</Badge>
                      ) : user.permissions.length === 0 ? (
                        <span className="text-muted-foreground">Nenhuma</span>
                      ) : (
                        <div className="flex flex-wrap gap-1">
                          {user.permissions.map((permission) => (
                            <Badge key={permission}>{permissionLabels[permission]}</Badge>
                          ))}
                        </div>
                      )}
                    </td>
                    <td className="px-4 py-3">
                      <Badge tone={user.isActive ? "success" : "neutral"}>{user.isActive ? "Ativo" : "Inativo"}</Badge>
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">{user.lastSignInAt ? formatDateTime(user.lastSignInAt) : "Nunca"}</td>
                    <td className="px-4 py-3">
                      <div className="flex justify-end gap-1">
                        <ActionButton label="Editar" onClick={() => setDialog({ kind: "edit", user })}>
                          <Pencil className="size-4" aria-hidden />
                        </ActionButton>
                        <ActionButton label="Definir nova senha" onClick={() => setDialog({ kind: "password", user })}>
                          <KeyRound className="size-4" aria-hidden />
                        </ActionButton>
                        {!isSuperAdmin && (
                          <ActionButton label={user.isActive ? "Desativar acesso" : "Reativar acesso"} onClick={() => void toggleActive(user)}>
                            {user.isActive ? <UserX className="size-4" aria-hidden /> : <UserCheck className="size-4" aria-hidden />}
                          </ActionButton>
                        )}
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      <UserFormDialog
        open={dialog?.kind === "create" || dialog?.kind === "edit"}
        user={dialog?.kind === "edit" ? dialog.user : undefined}
        onClose={closeDialog}
      />
      <ResetPasswordDialog user={dialog?.kind === "password" ? dialog.user : undefined} onClose={closeDialog} />
    </>
  );
}

function ActionButton({ label, onClick, children }: { label: string; onClick: () => void; children: ReactNode }) {
  return (
    <button
      type="button"
      title={label}
      aria-label={label}
      onClick={onClick}
      className="rounded-lg p-2 text-muted-foreground hover:bg-surface-muted hover:text-foreground"
    >
      {children}
    </button>
  );
}
