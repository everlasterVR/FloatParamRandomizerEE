#!/bin/bash

files=$(grep -o '<Compile Include="[^"]*"' FloatParamRandomizerEE.csproj | sed 's/<Compile Include="//; s/"//')
echo "$files" > FloatParamRandomizerEE.cslist
