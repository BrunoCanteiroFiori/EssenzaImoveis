"use client";

import { useState, type FormEvent } from "react";
import { useSearchParams } from "next/navigation";
import { Lock, LogIn } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Field, Input } from "@/components/ui/field";
import { Alert } from "@/components/ui/feedback";
import { ApiError } from "@/lib/api-client";
import { login } from "@/lib/auth.api";

const defaultDestination = "/admin/dashboard";

/** Só aceita voltar para páginas da área administrativa (evita redirecionamento aberto). */
function safeDestination(next: string | null) {
  return next && next.startsWith("/admin") && !next.startsWith("//") ? next : defaultDestination;
}

export function AuthComponent() {
  const searchParams = useSearchParams();
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    setLoading(true);
    setError(null);
    try {
      await login(String(data.get("email") ?? ""), String(data.get("password") ?? ""));
      // Navegação completa: a área administrativa sempre começa com a sessão recém-criada.
      window.location.assign(safeDestination(searchParams.get("next")));
    } catch (exception) {
      setError(exception instanceof ApiError ? exception.message : "Não foi possível conectar. Tente novamente.");
      setLoading(false);
    }
  }

  return (
    <div className="w-full max-w-sm rounded-2xl border bg-surface p-8 shadow-sm">
      <div className="mb-6 flex flex-col items-center gap-3 text-center">
        <span className="flex size-12 items-center justify-center rounded-xl bg-accent-soft text-accent">
          <Lock className="size-5" aria-hidden />
        </span>
        <h1 className="text-xl font-semibold">Área restrita</h1>
        <p className="text-sm text-muted-foreground">Acesso exclusivo da equipe da corretora.</p>
      </div>
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <Field label="E-mail" htmlFor="login-email">
          <Input id="login-email" name="email" type="email" required autoComplete="username" maxLength={200} />
        </Field>
        <Field label="Senha" htmlFor="login-password">
          <Input id="login-password" name="password" type="password" required autoComplete="current-password" maxLength={128} />
        </Field>
        {error && <Alert>{error}</Alert>}
        <Button type="submit" loading={loading}>
          <LogIn className="size-4" aria-hidden /> Entrar
        </Button>
      </form>
    </div>
  );
}
