"use client";

import { useState, type FormEvent } from "react";
import { useAuth } from "@/components/admin/auth-provider";
import { Button } from "@/components/ui/button";
import { Dialog } from "@/components/ui/dialog";
import { Field, Input } from "@/components/ui/field";
import { Alert } from "@/components/ui/feedback";
import type { Permission } from "@/lib/auth.api";
import { createUser, permissionLabels, permissions, resetUserPassword, updateUser, type User } from "@/lib/users.api";

type UserFormDialogProps = {
  open: boolean;
  user?: User;
  onClose: (changed: boolean) => void;
};

/** Cria ou edita um usuário da equipe e as permissões dele. */
export function UserFormDialog({ open, user, onClose }: UserFormDialogProps) {
  return (
    <Dialog open={open} title={user ? "Editar usuário" : "Novo usuário"} onClose={() => onClose(false)}>
      {open && <UserForm key={user?.id ?? "new"} user={user} onClose={onClose} />}
    </Dialog>
  );
}

function UserForm({ user, onClose }: { user?: User; onClose: (changed: boolean) => void }) {
  const { handleError } = useAuth();
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const isSuperAdmin = user?.role === "SuperAdmin";

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const selected = data.getAll("permissions").map(String) as Permission[];
    const name = String(data.get("name") ?? "");
    const email = String(data.get("email") ?? "");

    setSaving(true);
    setError(null);
    try {
      if (user) {
        await updateUser(user.id, { name, email, permissions: selected });
      } else {
        await createUser({ name, email, password: String(data.get("password") ?? ""), permissions: selected });
      }
      onClose(true);
    } catch (exception) {
      setError(handleError(exception));
      setSaving(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      <Field label="Nome" htmlFor="user-name">
        <Input id="user-name" name="name" required maxLength={100} defaultValue={user?.name} />
      </Field>
      <Field label="E-mail" htmlFor="user-email">
        <Input id="user-email" name="email" type="email" required maxLength={200} defaultValue={user?.email} autoComplete="off" />
      </Field>
      {!user && (
        <Field label="Senha inicial" htmlFor="user-password" hint="Mínimo de 10 caracteres, com letras e números. Informe à pessoa por um canal seguro.">
          <Input id="user-password" name="password" type="password" required minLength={10} maxLength={128} autoComplete="new-password" />
        </Field>
      )}
      <fieldset className="flex flex-col gap-2" disabled={isSuperAdmin}>
        <legend className="mb-1 text-sm font-medium">Permissões</legend>
        {isSuperAdmin ? (
          <p className="text-sm text-muted-foreground">O super admin sempre tem acesso total.</p>
        ) : (
          permissions.map((permission) => (
            <label key={permission} className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                name="permissions"
                value={permission}
                defaultChecked={user?.permissions.includes(permission) ?? permission === "ReadProperties"}
                className="size-4 accent-[var(--accent)]"
              />
              {permissionLabels[permission]}
            </label>
          ))
        )}
        {!isSuperAdmin && <p className="text-xs text-muted-foreground">Quem edita ou exclui imóveis também passa a vê-los na gestão.</p>}
      </fieldset>
      {error && <Alert>{error}</Alert>}
      <div className="flex justify-end gap-2">
        <Button variant="secondary" onClick={() => onClose(false)}>
          Cancelar
        </Button>
        <Button type="submit" loading={saving}>
          {user ? "Salvar" : "Criar usuário"}
        </Button>
      </div>
    </form>
  );
}

/** Define uma nova senha para o usuário (as sessões abertas dele deixam de valer). */
export function ResetPasswordDialog({ user, onClose }: { user?: User; onClose: (changed: boolean) => void }) {
  const { handleError } = useAuth();
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!user) return;
    setSaving(true);
    setError(null);
    try {
      await resetUserPassword(user.id, String(new FormData(event.currentTarget).get("newPassword") ?? ""));
      setSaving(false);
      onClose(true);
    } catch (exception) {
      setError(handleError(exception));
      setSaving(false);
    }
  }

  return (
    <Dialog open={user !== undefined} title={`Nova senha para ${user?.name ?? ""}`} onClose={() => onClose(false)}>
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <Field label="Nova senha" htmlFor="reset-password" hint="Mínimo de 10 caracteres, com letras e números.">
          <Input id="reset-password" name="newPassword" type="password" required minLength={10} maxLength={128} autoComplete="new-password" />
        </Field>
        {error && <Alert>{error}</Alert>}
        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={() => onClose(false)}>
            Cancelar
          </Button>
          <Button type="submit" loading={saving}>
            Definir senha
          </Button>
        </div>
      </form>
    </Dialog>
  );
}
