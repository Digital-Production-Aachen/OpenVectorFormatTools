FROM mcr.microsoft.com/dotnet/sdk:6.0 as build
# uses debian based image

WORKDIR /src

COPY ./* ./src/

RUN dotnet restore

COPY . .

RUN dotnet publish --no-restore -c Release -o /published src/FileReaderWriterFactoryGRPCWrapper.csproj

FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine as runtime

# Uncomment the line below if running with HTTPS
# ENV ASPNETCORE_URLS=https://+:443
ENV GRPC_SERVER_IP=0.0.0.0
ENV GRPC_SERVER_PORT=50051
ENV GRPC_SERVER_AUTOMATED_CACHING_THRESHOLD_BYTES=67108864

WORKDIR /app

COPY --from=build /published .

ENTRYPOINT [ "dotnet", "FileReaderWriterFactoryGRPCWrapper.dll" ]