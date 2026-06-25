# syntax=docker/dockerfile:1

FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json .npmrc ./
# --legacy-peer-deps: NgRx 21 peers on Angular 21 while we run Angular 22 (no NgRx 22 yet).
# The .npmrc copied above also sets this, but pass it explicitly so the build is self-evident.
RUN npm ci --legacy-peer-deps

COPY . .
RUN npm run build


FROM node:22-alpine AS final
WORKDIR /app
ENV NODE_ENV=production
ENV PORT=4000
EXPOSE 4000

COPY --from=build /app/dist/frontend ./dist/frontend

CMD ["node", "dist/frontend/server/server.mjs"]
