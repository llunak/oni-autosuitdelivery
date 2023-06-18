#! /bin/bash
name=AutoSuitDelivery
FrameworkPathOverride=$(dirname $(which mono))/../lib/mono/4.7.1-api/ DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet build $name.csproj /property:Configuration=Release
if test $? -eq 0; then
    # no idea why these get created, but they break game loading
    shopt -s extglob
    rm -f $(ls -1 ../*.dll | grep -v $name)
fi
version=$(cat $name.csproj | grep AssemblyVersion | sed 's#.*<AssemblyVersion>\(.*\)</AssemblyVersion>.*#\1#')
sed -i "s/version: .*/version: $version/" ../mod_info.yaml
