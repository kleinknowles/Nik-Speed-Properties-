FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY NikSpeed.Api.csproj ./
RUN dotnet restore NikSpeed.Api.csproj
COPY . ./
RUN dotnet publish NikSpeed.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
RUN mkdir -p /var/lib/nikspeed/uploads && chown $APP_UID:$APP_UID /var/lib/nikspeed/uploads
ENV Storage__UploadPath=/var/lib/nikspeed/uploads
EXPOSE 8080
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "NikSpeed.Api.dll"]
