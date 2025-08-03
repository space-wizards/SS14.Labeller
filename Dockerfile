FROM debian:bookworm-slim AS base
WORKDIR /app

ARG TARGETPLATFORM
ARG RID

RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

RUN echo "Building for platform: $TARGETPLATFORM" \
    && case "$TARGETPLATFORM" in \
    "linux/amd64") export RID=linux-x64 ;; \
    "linux/arm64") export RID=linux-arm64 ;; \
    *) echo "Unsupported TARGETPLATFORM: $TARGETPLATFORM" && exit 1 ;; \
    esac

COPY release/${RID}/ ./

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl --fail http://localhost:5000/ || exit 1

ENTRYPOINT ["./SS14.Labeller"]