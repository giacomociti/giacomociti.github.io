#!/usr/bin/env bash
echo "Formatting pages from fsx..."
dotnet fsi build.fsx

echo "Installing node packages..."
cd node/model-based-testing
npm install

echo "Running fable..."
fable --cwd src --run npm run build
