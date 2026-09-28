# syntax=docker/dockerfile:1

# -----------------------------------------------------------------------------
# Build stage: installs the dependencies and builds the Angular application.
# The output is static HTML, JavaScript and CSS, which does not depend on the
# CPU architecture, so this stage always runs natively on the build machine.
# -----------------------------------------------------------------------------
FROM --platform=$BUILDPLATFORM node:24-alpine AS build
WORKDIR /src

COPY package.json package-lock.json ./
RUN --mount=type=cache,target=/root/.npm \
    npm ci --no-audit --no-fund

ARG API_URL=https://api.pocketadvisor.codecameleon.com/api
COPY . .
RUN npx ng build --configuration production --define "API_URL='${API_URL}'"

# -----------------------------------------------------------------------------
# Runtime stage: only nginx and the built static files.
# Node.js, node_modules and the source code are left behind.
# -----------------------------------------------------------------------------
FROM nginxinc/nginx-unprivileged:1.30-alpine AS runtime

ARG APP_VERSION=development
ARG API_URL=https://api.pocketadvisor.codecameleon.com/api
LABEL org.opencontainers.image.title="PocketAdvisor Web" \
      org.opencontainers.image.version=$APP_VERSION \
      org.opencontainers.image.source="https://github.com/CodeCameleon/PocketAdvisor"
ENV APP_VERSION=$APP_VERSION \
    API_URL=$API_URL

COPY nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /src/dist/PocketAdvisor.Client/browser /usr/share/nginx/html

EXPOSE 8080
