# Set the base image as the .NET 8.0 SDK (this includes the runtime)
FROM mcr.microsoft.com/dotnet/sdk:8.0 as build-env

# Copy everything and publish the release (publish implicitly restores and builds)
WORKDIR /app
COPY . ./
RUN dotnet publish ./NugetReport/NugetReport.csproj -c Release -o out --no-self-contained

# Label the container
LABEL maintainer="Mathijs Nabbe <mathijs.nabbe@gmail.com>"
LABEL repository="https://github.com/MathijsNabbe/actions-nuget-report"
LABEL homepage="https://github.com/MathijsNabbe/actions-nuget-report"
LABEL com.github.actions.name="NugetReport"
LABEL com.github.actions.description="A github actions step used to detect all NuGet references in a .NET project, and create a detailed report in the Actions summary."
# See branding:
# https://docs.github.com/actions/creating-actions/metadata-syntax-for-github-actions#branding
LABEL com.github.actions.icon="file-text"
LABEL com.github.actions.color="white"

# Relayer the .NET SDK, anew with the build output
FROM mcr.microsoft.com/dotnet/sdk:8.0
COPY --from=build-env /app/out .
ENTRYPOINT [ "dotnet", "/NugetReport.dll" ]