# Tactics & Command Dynamics dedicated server.
#
#     docker compose up -d --build
#
# The server speaks OpenRA's own protocol over TCP, not HTTP, so a reverse proxy
# has nothing to route. Expose port 1234 as a raw TCP port.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# A client will not join a server whose version string differs from its own, and
# an unstamped build calls itself {DEV_VERSION}. Stamp the release your players
# are actually running or their AppImage bounces off this server.
ARG VERSION=release-tcd-0.2.0

RUN apt-get update \
	&& apt-get install -y --no-install-recommends make \
	&& rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY . .
RUN make version VERSION="${VERSION}" && make

FROM mcr.microsoft.com/dotnet/runtime:10.0

# curl and unzip fetch the freeware Red Alert package on first start. The .mix
# files are Westwood's and are never committed to this repository; the container
# downloads them the same way the game itself does.
RUN apt-get update \
	&& apt-get install -y --no-install-recommends ca-certificates curl unzip \
	&& rm -rf /var/lib/apt/lists/*

WORKDIR /opt/openra-tcd

COPY --from=build /src/bin ./bin
COPY --from=build /src/mods ./mods
COPY --from=build /src/glsl ./glsl
COPY --from=build /src/launch-dedicated.sh ./launch-dedicated.sh
COPY docker/entrypoint.sh /usr/local/bin/entrypoint.sh

RUN chmod +x ./launch-dedicated.sh /usr/local/bin/entrypoint.sh

# Game content, replays and downloaded maps. Keep it across rebuilds or every
# deploy re-downloads a few hundred megabytes.
VOLUME /data

EXPOSE 1234/tcp

ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
