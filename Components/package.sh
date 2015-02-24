#!/bin/sh

VERSION=$1
if [ -z "$VERSION" ]; then
    echo "Must specify version."
    exit 1
fi

zip LiveSplit.Kotor2_v${VERSION}.zip LiveSplit.Kotor2.dll ../readme.txt
