#!/bin/sh
# Render sets PORT at runtime; ASP.NET Core needs ASPNETCORE_URLS to bind to it.
export ASPNETCORE_URLS="http://0.0.0.0:${PORT:-5000}"
exec dotnet PlaceNamesBlazor.dll
