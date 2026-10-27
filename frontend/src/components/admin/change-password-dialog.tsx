"use client";

import { useState, type FormEvent } from "react";
import { Button } from "@/components/ui/button";
import { Dialog } from "@/components/ui/dialog";
import { Field, Input } from "@/components/ui/field";
import { Alert } from "@/components/ui/feedback";
import { changeOwnPassword } from "@/lib/auth.api";
import { useAuth } from "./auth-provider";

/** Troca a própria senha. Depois da troca, a API encerra as sessões e é preciso entrar de novo. */
export function ChangePasswordDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { handleError, logout } = useAuth();
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const newPassword = String(data.get("newPassword") ?? "");
    if (newPassword !== String(data.get("confirmPassword") ?? "")) {
      setError("A confirmação não confere com a nova senha.");
      return;
    }

    setSaving(true);
    setError(null);
    try {
      await changeOwnPassword(String(data.get("currentPassword") ?? ""), newPassword);
      await logout();
    } catch (exception) {
      setError(handleError(exception));
      setSaving(false);
    }
  }

  return (
    <Dialog open={open} title="Alterar minha senha" onClose={onClose}>
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <Field label="Senha atual" htmlFor="current-password">
          <Input id="current-password" name="currentPassword" type="password" required autoComplete="current-password" />
        </Field>
        <Field label="Nova senha" htmlFor="new-password" hint="Mínimo de 10 caracteres, com letras e números.">
          <Input id="new-password" name="newPassword" type="password" required minLength={10} maxLength={128} autoComplete="new-password" />
        </Field>
        <Field label="Confirmar nova senha" htmlFor="confirm-password">
          <Input id="confirm-password" name="confirmPassword" type="password" required autoComplete="new-password" />
        </Field>
        {error && <Alert>{error}</Alert>}
        <p className="text-xs text-muted-foreground">Por segurança, você precisará entrar novamente após a troca.</p>
        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" loading={saving}>
            Alterar senha
          </Button>
        </div>
      </form>
    </Dialog>
  );
}
