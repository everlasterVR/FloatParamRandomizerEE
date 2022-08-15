#!/bin/bash
creator_name=everlaster
plugin_name=FloatParamRandomizerEE
package_version=$1
plugin_version=$(git tag --points-at HEAD)

[ -z "$package_version" ] && printf "Usage: ./package.sh [var package version]\n" && exit 1
[ -z "$plugin_version" ] && printf "Git tag not set on current commit.\n" && exit 1

# Setup archive contents
publish_dir=publish/Custom/Scripts/$creator_name/$plugin_name
mkdir -p $publish_dir
cp meta.json publish/
cp *.cslist $publish_dir/
cp -r vendor $publish_dir/
cp -r src $publish_dir/

# Update version info
sed -i "s/v0\.0\.0/v$plugin_version/g" publish/meta.json
sed -i "s/v0\.0\.0/v$plugin_version/g" $publish_dir/src/$plugin_name.cs

# hide .cs files (plugin is loaded with .cslist)
for file in $(find $publish_dir -type f -name "*.cs"); do
    touch $file.hide
done

# Zip files to .var and cleanup
cd publish
package="$creator_name.$plugin_name.$package_version.var"
zip -r $package *
cd ..
mkdir -p ../../../../AddonPackages/Self
mv publish/$package ../../../../AddonPackages/Self
rm -rf publish

echo "Package $package created with version $plugin_version and moved to AddonPackages/Self."
