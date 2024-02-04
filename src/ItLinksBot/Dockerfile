#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
ARG \
    # Platform of the build result. Eg linux/amd64, linux/arm/v7, windows/amd64
    TARGETPLATFORM \
    # Architecture component of TARGETPLATFORM
    TARGETARCH \
    # Platform of the node performing the build
    BUILDPLATFORM
COPY ["ItLinksBot/ItLinksBot.csproj", "ItLinksBot/"]
RUN echo "${BUILDPLATFORM} - ${TARGETARCH}" && dotnet restore "ItLinksBot/ItLinksBot.csproj" --locked-mode -a $TARGETARCH
COPY . .
WORKDIR "/src/ItLinksBot"
RUN echo "${BUILDPLATFORM} - ${TARGETARCH}" && dotnet build "ItLinksBot.csproj" -c Release -o /app/build -a $TARGETARCH

FROM build AS publish
RUN echo "${BUILDPLATFORM} - ${TARGETARCH}" && dotnet publish "ItLinksBot.csproj" -c Release -o /app/publish -a $TARGETARCH

FROM base AS final
RUN groupadd --gid 10001 dotnet \
    && useradd --uid 10000 --gid dotnet --shell /bin/bash --create-home dotnet \
    && mkdir -p /app/db/src \
    && chown -R dotnet:dotnet /app
WORKDIR /app
USER dotnet
COPY --from=publish /app/publish .
HEALTHCHECK NONE
ENTRYPOINT ["dotnet", "ItLinksBot.dll"]