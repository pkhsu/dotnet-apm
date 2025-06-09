# syntax=docker/dockerfile:1

# --- Build Stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 複製專案檔 (.csproj)
COPY ["src/Sample.Api/Sample.Api.csproj", "src/Sample.Api/"]

# 還原 NuGet 套件 (利用 Docker Layer Caching)
RUN dotnet restore "src/Sample.Api/Sample.Api.csproj"

# 複製所有剩餘的原始碼
COPY . .
WORKDIR "/src/src/Sample.Api"

# 建構並發布應用程式
RUN dotnet publish "Sample.Api.csproj" -c Release -o /app/publish

# --- Final Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Sample.Api.dll"] 