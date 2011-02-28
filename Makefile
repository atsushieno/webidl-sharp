all: webidl-sharp.dll webgl-idl-parser.exe

webgl-idl-parser.exe : webgl-idl-parser.cs webidl-sharp.dll
	mcs webgl-idl-parser.cs -r:webidl-sharp.dll -r:Irony.dll -debug

webidl-sharp.dll : webidl-parser.cs
	mcs webidl-parser.cs -t:library -out:webidl-sharp.dll -debug -r:Irony.dll

data: signatures.xml

signatures.xml: webgl-idl-parser.exe webgl.idl
	mono --debug -O=-all ./webgl-idl-parser.exe webgl.idl > signatures.xml

clean:
	rm webidl-sharp.dll webidl-sharp.dll.mdb
	rm webgl-idl-parser.exe webgl-idl-parser.exe.mdb
	rm signatures.xml
