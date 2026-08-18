FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app

RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080
EXPOSE 55901
EXPOSE 55902
EXPOSE 55903
EXPOSE 55904
EXPOSE 55905
EXPOSE 55906
EXPOSE 44405
EXPOSE 55980

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY Directory.Packages.props .
COPY Directory.Build.props .

COPY Startup/*.csproj Startup/
COPY Persistence/**/*.csproj Persistence/

RUN dotnet restore "Startup/MUnique.OpenMU.Startup.csproj"

COPY . .

WORKDIR /src/Startup

RUN dotnet build "MUnique.OpenMU.Startup.csproj" \
    -c Release \
    -o /app/build \
    -p:ci=true

FROM build AS publish
RUN dotnet publish "MUnique.OpenMU.Startup.csproj" \
    -c Release \
    -o /app/publish \
    -p:ci=true \
    -p:DebugType=none \
    -p:DebugSymbols=false \
    --self-contained false

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

# Strip build-only artifacts (~4 MB):
#   *.pdb  ~4 MB (debug symbols)
# NOTE: Do NOT delete Microsoft.CodeAnalysis*.dll — OpenMU uses Roslyn to
# compile plugins at runtime (plug-in system), and removing it breaks bot
# generation and other dynamic plugins (FileNotFoundException at startup).
RUN find /app -name '*.pdb' -delete

RUN mkdir -p /app/logs && chmod 777 /app/logs

ARG APP_UID=1000
USER ${APP_UID}

ENTRYPOINT ["dotnet", "MUnique.OpenMU.Startup.dll", "-autostart"]