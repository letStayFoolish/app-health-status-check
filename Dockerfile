FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Non-root user that will run chrome
RUN groupadd -g 1000 ttuser \
 && useradd -m -u 1000 -g ttuser ttuser

# Pin a specific Chrome version
ARG CHROME_VERSION=139.0.7258.138-1

# Install Chrome
RUN set -eux; \
    apt-get update; \
    apt-get install -y --no-install-recommends wget ca-certificates gnupg \
        fonts-liberation fonts-noto-color-emoji fonts-dejavu libxss1; \
    wget -q -O /tmp/google-chrome-stable_${CHROME_VERSION}_amd64.deb \
      "https://dl.google.com/linux/chrome/deb/pool/main/g/google-chrome-stable/google-chrome-stable_${CHROME_VERSION}_amd64.deb"; \
    dpkg -i /tmp/google-chrome-stable_${CHROME_VERSION}_amd64.deb || apt-get -fy install; \
    rm -f /tmp/google-chrome-stable_${CHROME_VERSION}_amd64.deb; \
    rm -rf /var/lib/apt/lists/*; \
    google-chrome --version

# Point PuppeteerSharp to system Chrome; keep Chrome caches writable
ENV PuppeteerSettings__LaunchOptions__ExecutablePath=/usr/bin/google-chrome-stable

# https://github.com/puppeteer/puppeteer/issues/11023
ENV XDG_CONFIG_HOME=/tmp/.chromium \
    XDG_CACHE_HOME=/tmp/.chromium

# App data dir (e.g., SQLite)
RUN mkdir -p /data && chown ttuser:ttuser /data

# ----- Build & publish -----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY nuget.config .
COPY DeepCheck/DeepCheck.csproj DeepCheck/DeepCheck.csproj
RUN dotnet restore DeepCheck/DeepCheck.csproj
COPY . .
WORKDIR /src/DeepCheck
RUN dotnet publish DeepCheck.csproj -c Release -o /out /p:UseAppHost=false

# ----- Final image -----
FROM runtime AS final
WORKDIR /app
COPY --from=build --chown=ttuser:ttuser /out .

ARG PRODUCT_VERSION=v1.0.0
ARG FILE_VERSION=v1.0.0
ENV ConnectionStrings__DeepCheckDb="Data Source=/data/app.db" \
    PRODUCT_VERSION=$PRODUCT_VERSION \
    FILE_VERSION=$FILE_VERSION \
    ASPNETCORE_ENVIRONMENT=Development \
    ASPNETCORE_URLS="http://+:5205"

USER ttuser
EXPOSE 5205
ENTRYPOINT ["dotnet", "DeepCheck.dll"]
