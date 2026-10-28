#!/usr/bin/env bash
# Executado no servidor pelo workflow .github/workflows/deploy.yml, dentro de DEPLOY_PATH.
# Recebe IMAGE_PREFIX, IMAGE_TAG e REGISTRY_USER por variável de ambiente e o token do registry pela entrada padrão.
set -euo pipefail
cd "$(dirname "$0")"

: "${IMAGE_PREFIX:?IMAGE_PREFIX não informado}"
: "${IMAGE_TAG:?IMAGE_TAG não informado}"
: "${REGISTRY_USER:?REGISTRY_USER não informado}"

if [ ! -f .env ]; then
  echo "Arquivo .env não encontrado em $(pwd). Crie a partir do .env.example (veja deploy/README.md)." >&2
  exit 1
fi

export IMAGE_PREFIX IMAGE_TAG

docker login ghcr.io --username "$REGISTRY_USER" --password-stdin
trap 'docker logout ghcr.io > /dev/null' EXIT

docker compose pull
docker compose up --detach --remove-orphans

echo "Aguardando a API e o site responderem..."
for _ in $(seq 1 30); do
  if docker compose exec -T caddy wget -q --spider http://api:4000/health \
    && docker compose exec -T caddy wget -q --spider http://web:3000/; then
    echo "Deploy da versão $IMAGE_TAG concluído."
    docker image prune --all --force --filter "until=168h" > /dev/null
    exit 0
  fi
  sleep 2
done

echo "A API ou o site não responderam após o deploy. Últimos logs:" >&2
docker compose logs --tail 100 migrations api web >&2
exit 1
