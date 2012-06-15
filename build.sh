sh compile.sh

export AS="as -arch i386"
export CC="cc -arch i386"
mkbundle -o longbow-bin longbow.exe --deps
