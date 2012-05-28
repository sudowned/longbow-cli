gmcs longbow.cs -r:System.Xml.Linq.dll,System.Web.dll

read COUNTER < longbow.vr
COUNTER=$((COUNTER+1))

echo $COUNTER > longbow.vr
