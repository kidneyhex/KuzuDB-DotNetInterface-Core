REM STEP 1) refresh.bet ** YOU ARE HERE
REM STEP 2) wrapperlib\rebuild.bat
REM STEP 3) overwrite.bat

REM Clear out old generated files before inserting new ones
@del KuzuFiles\generated_classes\*.* /Q

REM Get Swig from here: https://www.swig.org/download.html
REM TODO: Update this to your path to swig
p:\home\tools\swigwin\swig.exe -c++ -csharp -IKuzuFiles/ -outdir "KuzuFiles/generated_classes/" -o wrapperlib/kuzu_wrap.cpp SWIG-InputFile/kuzu.i