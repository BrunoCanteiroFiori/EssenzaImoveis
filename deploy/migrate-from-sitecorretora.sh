#!/usr/bin/env bash
# Migração única do nome antigo do projeto (sitecorretora) para essenzaimoveis. Chamado pelo deploy.sh, dentro de
# DEPLOY_PATH, depois de baixar as imagens novas e antes de subir os containers.
#
# Só age se existe o volume do banco antigo e ainda não existe o novo:
#   1. para e remove os containers antigos (libera as portas 80, 443 e 8080);
#   2. copia o volume do banco para o volume do projeto novo;
#   3. renomeia o banco e o usuário antigos para POSTGRES_DB e POSTGRES_USER do .env (a senha continua a mesma).
# Os volumes antigos ficam intactos como backup. Se algo falhar, o volume novo é apagado e a próxima execução tenta de novo.
set -euo pipefail
cd "$(dirname "$0")"

readonly old_project=sitecorretora
readonly new_project=essenzaimoveis
readonly old_volume="${old_project}_postgres-data"
readonly new_volume="${new_project}_postgres-data"
readonly old_container="${old_project}-postgres-1"
readonly rename_container="${new_project}-migracao"
readonly temporary_role=migracao_temporaria
readonly postgres_image=postgres:18

if ! docker volume inspect "$old_volume" > /dev/null 2>&1 || docker volume inspect "$new_volume" > /dev/null 2>&1; then
  exit 0
fi

# Valor de uma chave do .env gerado pelo workflow (formato CHAVE='valor').
read_env() {
  sed -n "s/^$1='\(.*\)'\$/\1/p" .env
}

# Valor de uma variável de ambiente do container antigo do Postgres (vazio se o container não existe mais).
read_old_container_env() {
  docker inspect "$old_container" --format '{{range .Config.Env}}{{println .}}{{end}}' 2> /dev/null | sed -n "s/^$1=//p" || true
}

new_user=$(read_env POSTGRES_USER)
new_database=$(read_env POSTGRES_DB)
old_user=$(read_old_container_env POSTGRES_USER)
old_database=$(read_old_container_env POSTGRES_DB)
old_user=${old_user:-corretora}
old_database=${old_database:-corretora}

run_sql() {
  docker exec "$rename_container" psql -v ON_ERROR_STOP=1 -q -U "$1" -d postgres -c "$2"
}

migrate() {
  echo "Migrando ${old_project} -> ${new_project}: parando os containers antigos..."
  docker ps --all --quiet --filter "label=com.docker.compose.project=${old_project}" | xargs -r docker rm --force > /dev/null || return 1
  docker network rm "${old_project}_default" > /dev/null 2>&1 || true

  echo "Copiando o volume ${old_volume} para ${new_volume}..."
  docker volume create \
    --label "com.docker.compose.project=${new_project}" \
    --label com.docker.compose.volume=postgres-data \
    "$new_volume" > /dev/null || return 1
  docker run --rm --volume "${old_volume}:/from:ro" --volume "${new_volume}:/to" --entrypoint sh "$postgres_image" \
    -c 'cp -a /from/. /to/' || return 1

  echo "Renomeando o banco ${old_database} -> ${new_database} e o usuário ${old_user} -> ${new_user}..."
  docker run --detach --name "$rename_container" --volume "${new_volume}:/var/lib/postgresql" "$postgres_image" > /dev/null || return 1
  for _ in $(seq 1 30); do
    if docker exec "$rename_container" pg_isready --quiet --username "$old_user"; then
      break
    fi
    sleep 1
  done

  # O usuário conectado não pode renomear a si mesmo: um superusuário temporário faz a troca e depois é removido.
  run_sql "$old_user" "CREATE ROLE ${temporary_role} SUPERUSER LOGIN" || return 1
  if [ "$old_database" != "$new_database" ]; then
    run_sql "$temporary_role" "ALTER DATABASE \"${old_database}\" RENAME TO \"${new_database}\"" || return 1
  fi
  if [ "$old_user" != "$new_user" ]; then
    run_sql "$temporary_role" "ALTER ROLE \"${old_user}\" RENAME TO \"${new_user}\"" || return 1
  fi
  run_sql "$new_user" "DROP ROLE ${temporary_role}" || return 1
  docker rm --force "$rename_container" > /dev/null || return 1
}

if ! migrate; then
  docker rm --force "$rename_container" > /dev/null 2>&1 || true
  docker volume rm "$new_volume" > /dev/null 2>&1 || true
  echo "A migração de ${old_project} para ${new_project} falhou. O volume ${old_volume} continua intacto." >&2
  exit 1
fi

echo "Migração concluída. Os volumes ${old_project}_* ficam como backup (veja deploy/README.md)."
