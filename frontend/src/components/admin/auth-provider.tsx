"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { ApiError } from "@/lib/api-client";
import { getCurrentUser, logout as logoutRequest, type AuthUser, type Permission } from "@/lib/auth.api";

type AuthContextValue = {
  user: AuthUser | null;
  loading: boolean;
  isSuperAdmin: boolean;
  hasPermission: (permission: Permission) => boolean;
  /** Trata erros de chamadas à API: em 401 encerra a sessão local (o layout redireciona para o login). */
  handleError: (error: unknown) => string;
  logout: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    getCurrentUser()
      .then((current) => active && setUser(current))
      .catch(() => active && setUser(null))
      .finally(() => active && setLoading(false));
    return () => {
      active = false;
    };
  }, []);

  const handleError = useCallback((error: unknown) => {
    if (error instanceof ApiError) {
      if (error.status === 401) setUser(null);
      return error.message;
    }
    return "Não foi possível concluir a operação. Verifique sua conexão.";
  }, []);

  const logout = useCallback(async () => {
    try {
      await logoutRequest();
    } finally {
      setUser(null);
    }
  }, []);

  const value = useMemo<AuthContextValue>(() => {
    const isSuperAdmin = user?.role === "SuperAdmin";
    return {
      user,
      loading,
      isSuperAdmin,
      hasPermission: (permission) => isSuperAdmin || (user?.permissions.includes(permission) ?? false),
      handleError,
      logout,
    };
  }, [user, loading, handleError, logout]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth precisa estar dentro de <AuthProvider>.");
  return context;
}
