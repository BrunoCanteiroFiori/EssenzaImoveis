"use client";

import { useCallback, useEffect, useState } from "react";
import { useAuth } from "./auth-provider";

type State<T> = { data?: T; error?: string; loading: boolean };

/**
 * Carrega dados da API e recarrega quando <paramref name="load"/> muda (use useCallback com os filtros).
 * Em 401 a sessão é encerrada pelo AuthProvider.
 */
export function useApiData<T>(load: () => Promise<T>) {
  const { handleError } = useAuth();
  const [state, setState] = useState<State<T>>({ loading: true });
  const [version, setVersion] = useState(0);

  useEffect(() => {
    let active = true;
    load()
      .then((data) => active && setState({ data, loading: false }))
      .catch((error: unknown) => active && setState({ error: handleError(error), loading: false }));
    return () => {
      active = false;
    };
  }, [load, version, handleError]);

  const reload = useCallback(() => setVersion((current) => current + 1), []);
  const setData = useCallback((data: T) => setState({ data, loading: false }), []);

  return { ...state, reload, setData };
}
