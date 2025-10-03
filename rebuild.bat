@cd wrapperlib
@del build\*.* /q
@cd build
cmake -G Ninja .. -DCMAKE_BUILD_TYPE=Release
ninja
@cd ..\..