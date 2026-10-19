# Build de la versión web del frontend. El contexto de build es la carpeta
# frontend/. La dirección de la API queda grabada al compilar, por eso se
# recibe como argumento (VITE_API_URL).

FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
ARG VITE_API_URL
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

FROM caddy:2-alpine AS final
COPY Caddyfile /etc/caddy/Caddyfile
COPY --from=build /app/dist /srv

EXPOSE 80
