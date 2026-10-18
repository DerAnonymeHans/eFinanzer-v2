# =========================================
# Stage 1: Build the React (Vite) Application
# =========================================

ARG NODE_VERSION=26.12.0-alpine
# Use a lightweight Node.js image for building (customizable via ARG)
FROM node:${NODE_VERSION} AS builder

# Set the working directory inside the container
WORKDIR /app

# Copy package-related files first to leverage Docker's caching mechanism
COPY package.json package-lock.json ./

# Install project dependencies using npm ci (ensures a clean, reproducible install)
RUN --mount=type=cache,target=/root/.npm npm ci

# Copy the rest of the application source code into the container.
# .env and .env.production are included, Vite builds their VITE_ values into the bundle (see .dockerignore)
COPY . .

# Build the React.js application (outputs to /app/dist)
RUN npm run build


# =========================================
# Stage 2: Serve the static files with nginx
# =========================================
# Runs nginx as a non-root user, so it cannot bind port 80 and listens on 3000 instead (see nginx.conf)
FROM nginxinc/nginx-unprivileged:alpine AS runner

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --link --from=builder /app/dist /usr/share/nginx/html

# The reverse proxy forwards to efinanzer_app:3000 (see src/deploy/nginx.conf)
EXPOSE 3000
