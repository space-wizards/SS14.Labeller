FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build
ARG TARGETPLATFORM
WORKDIR /src

# for NativeAOT we need to install a platform linker
RUN apt-get update && \
    apt-get install -y clang zlib1g-dev && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

COPY . .

# Map the Docker TARGETPLATFORM to .NET's RID
# defaults to linux-x64 if unknown
RUN echo "Building for platform: $TARGETPLATFORM" \
    && case "$TARGETPLATFORM" in \
    "linux/amd64") export RID=linux-x64 ;; \
    "linux/arm64") export RID=linux-arm64 ;; \
    *) echo "Unsupported TARGETPLATFORM: $TARGETPLATFORM" && exit 1 ;; \
    esac \
    && dotnet publish ./SS14.Labeller -c Release -r $RID --self-contained true /p:PublishAot=true -o /app

FROM debian:bookworm-slim AS final
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl --fail http://localhost:5000/ || exit 1

ENTRYPOINT ["./SS14.Labeller"]
