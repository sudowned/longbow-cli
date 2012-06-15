rm -rf binary

sh build.sh

mkdir -p binary/longbow
##cp longbow.exe binary/longbow/
cp longbow-bin binary/longbow/longbow
cp longbow.vr binary/longbow/
##cp longbow binary/longbow/
cd binary/
zip -mr longbow.zip longbow
