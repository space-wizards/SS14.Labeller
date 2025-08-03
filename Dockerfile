FROM debian:bookworm-slim AS base
WORKDIR /app

ARG TARGETPLATFORM
ARG RID

RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY release/${RID}/ ./

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl --fail http://localhost:5000/ || exit 1

RUN chmod +x SS14.Labeller
ENTRYPOINT ["./SS14.Labeller"]