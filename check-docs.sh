#!/bin/bash
set -euo pipefail

cd site
dotnet tool restore
dotnet tool run lunet --stacktrace build
