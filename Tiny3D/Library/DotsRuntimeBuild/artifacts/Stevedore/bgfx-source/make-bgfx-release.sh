#!/bin/bash

DEST=bgfx-source

rm -rf ${DEST}
mkdir ${DEST}
for dep in bgfx bx bimg ; do
    if [[ ! -z $(git -C $dep status -s) ]] ; then
        echo "$dep is dirty, aborting"
        exit 1
    fi
    echo "$dep: `git -C $dep describe --always`" >> ${DEST}/VERSIONS
done

# copy this script in
cp make-bgfx-release.sh ${DEST}

cat ${DEST}/VERSIONS

for dep in bgfx bx bimg ; do
    echo "Copying $dep..."
    mkdir ${DEST}/$dep
    git -C $dep archive HEAD | tar -x -C ${DEST}/$dep
done

# delete some things we don't want to package that are huge
rm -rf ${DEST}/bgfx/examples
rm -rf ${DEST}/bgfx/3rdparty/spirv-tools/test
rm -rf ${DEST}/bgfx/3rdparty/glslang/Test

# and some things that we can't redistribute!
rm -rf ${DEST}/bgfx/3rdparty/dxsdk

echo "Created bgfx-source"
echo "Now run:"
echo "   bee steve pack bgfx-source"
echo "use artifact name: bgfx-source"
echo "use artifact version: `date +%Y%m%d00`"

