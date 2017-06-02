set -ex

echo "Attempting to fix NuGet package archive"
buildDir=$1
cd $buildDir

# Get the full package name. This will only fix one package if there are multiple in the directory
pkg=$(find *.nupkg | head -1)

mv $pkg $pkg.bak

fix7z () {
    # using the p7zip package because using zip and unzip doesn't fix the publish
    echo "Recompressing package with 7z"
    7z x $pkg.bak -opackage
    cd package
    7z a -tzip -r ../$pkg .
}

fixDefault () {
    echo "Recompressing package with unzip/zip"
    unzip $pkg.bak -d package
    cd package
    zip -r ../$pkg .
}

if [ -z "$NUGET_TEST_USE_DEFAULT_ZIP" ]; then
    fix7z
else
    fixDefault
fi
