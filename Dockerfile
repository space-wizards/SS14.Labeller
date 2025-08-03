FROM debian:bookworm-slim AS base
WORKDIR /app

ARG TARGETPLATFORM

COPY release/linux-x64/ ./linux-x64/
COPY release/linux-arm64/ ./linux-arm64/

# this giga sucks but i dont fucking cary anymore i have been at this for like 3 hours why is docker so bad how do people use this????

RUN case "${TARGETPLATFORM}" in \
      "linux/amd64") cp ./linux-x64/SS14.Labeller ./ ;; \
      "linux/arm64") cp ./linux-arm64/SS14.Labeller ./ ;; \
      *) echo "Unsupported platform: ${TARGETPLATFORM}" && exit 1 ;; \
    esac

RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl --fail http://localhost:5000/ || exit 1

RUN chmod +x SS14.Labeller
ENTRYPOINT ["./SS14.Labeller"]